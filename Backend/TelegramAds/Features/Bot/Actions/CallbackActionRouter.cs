using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramAds.Features.Bot.Chat;
using TelegramAds.Features.Bot.Notifications;
using TelegramAds.Features.Deals;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Bot.Actions;

public sealed class CallbackActionRouter
{
    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _db;
    private readonly BotNotificationService _notificationService;
    private readonly ChatHandler _chatHandler;
    private readonly CreativeFlowHandler _creativeFlowHandler;
    private readonly IClock _clock;
    private readonly ILogger<CallbackActionRouter> _logger;

    public CallbackActionRouter(
        ITelegramBotClient botClient,
        AppDbContext db,
        BotNotificationService notificationService,
        ChatHandler chatHandler,
        CreativeFlowHandler creativeFlowHandler,
        IClock clock,
        ILogger<CallbackActionRouter> logger)
    {
        _botClient = botClient;
        _db = db;
        _notificationService = notificationService;
        _chatHandler = chatHandler;
        _creativeFlowHandler = creativeFlowHandler;
        _clock = clock;
        _logger = logger;
    }

    public async Task RouteAsync(CallbackQuery query, CancellationToken ct)
    {
        var parts = query.Data!.Split(':');
        if (parts.Length < 2)
        {
            _logger.LogWarning("Invalid callback data format: {Data}", query.Data);
            return;
        }

        var action = parts[0];
        var payload = parts[1];

        try
        {
            switch (action)
            {
                case "accept_proposal":
                    await HandleAcceptProposalAsync(query, Guid.Parse(payload), ct);
                    break;
                case "reject_proposal":
                    await HandleRejectProposalAsync(query, Guid.Parse(payload), ct);
                    break;
                case "approve_creative":
                    await HandleApproveCreativeAsync(query, Guid.Parse(payload), ct);
                    break;
                case "request_edits":
                    await HandleRequestEditsAsync(query, Guid.Parse(payload), ct);
                    break;
                case "self_approve_creative":
                    await _creativeFlowHandler.HandleSelfApproveAsync(query, Guid.Parse(payload), ct);
                    break;
                case "self_reject_creative":
                    await _creativeFlowHandler.HandleSelfRejectAsync(query, Guid.Parse(payload), ct);
                    break;
                case "peer_approve_creative":
                    await _creativeFlowHandler.HandlePeerApproveAsync(query, Guid.Parse(payload), ct);
                    break;
                case "peer_reject_creative":
                    await _creativeFlowHandler.HandlePeerRejectAsync(query, Guid.Parse(payload), ct);
                    break;
                case "chat_select":
                    await _chatHandler.HandleChatSelectAsync(query, Guid.Parse(payload), ct);
                    break;
                case "chat_start":
                    await _chatHandler.HandleStartChatAsync(query, Guid.Parse(payload), ct);
                    break;
                default:
                    _logger.LogWarning("Unknown callback action: {Action}", action);
                    break;
            }

            await _botClient.AnswerCallbackQuery(query.Id, cancellationToken: ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict handling callback {Action} - likely duplicate action", action);
            await _botClient.AnswerCallbackQuery(query.Id, "This action has already been processed.", showAlert: true, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling callback {Action}", action);
            await _botClient.AnswerCallbackQuery(query.Id, "An error occurred", showAlert: true, cancellationToken: ct);
        }
    }

    private async Task HandleAcceptProposalAsync(CallbackQuery query, Guid proposalId, CancellationToken ct)
    {
        var proposal = await _db.DealProposals
            .Include(p => p.Deal)
                .ThenInclude(d => d.Channel)
            .Include(p => p.Deal)
                .ThenInclude(d => d.AdvertiserUser)
            .Include(p => p.Deal)
                .ThenInclude(d => d.ChannelOwnerUser)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ Proposal not found.", cancellationToken: ct);
            return;
        }

        if (proposal.Status != ProposalStatus.Pending)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ This proposal has already been responded to.", cancellationToken: ct);
            return;
        }

        proposal.Status = ProposalStatus.Accepted;
        proposal.RespondedAt = _clock.UtcNow;

        var deal = proposal.Deal;
        DealStateMachine.ValidateTransition(deal.Status, DealStatus.Agreed);
        deal.Status = DealStatus.Agreed;
        deal.AgreedPriceInTon = proposal.ProposedPriceInTon;
        deal.ScheduledPostTime = proposal.ProposedSchedule;
        deal.UpdatedAt = _clock.UtcNow;

        deal.Events.Add(new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            EventType = DealEventType.ProposalAccepted,
            FromStatus = DealStatus.Agreed,
            ToStatus = DealStatus.Agreed,
            ActorUserId = GetUserIdFromQuery(query),
            PayloadJson = $"{{\"proposalId\":\"{proposalId}\"}}",
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        await _botClient.SendMessage(
            query.Message!.Chat.Id,
            "✅ Proposal accepted! The deal is now agreed.\n\nAwaiting payment to the escrow address.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        var otherPartyTgId = proposal.ProposerUserId == deal.AdvertiserUserId
            ? deal.ChannelOwnerUser.TgUserId
            : deal.AdvertiserUser.TgUserId;

        await _notificationService.SendProposalAcceptedAsync(
            otherPartyTgId,
            deal.Id,
            deal.Channel.Title,
            proposal.ProposedPriceInTon);
    }

    private async Task HandleRejectProposalAsync(CallbackQuery query, Guid proposalId, CancellationToken ct)
    {
        var proposal = await _db.DealProposals
            .Include(p => p.Deal)
                .ThenInclude(d => d.Channel)
            .Include(p => p.Deal)
                .ThenInclude(d => d.AdvertiserUser)
            .Include(p => p.Deal)
                .ThenInclude(d => d.ChannelOwnerUser)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);

        if (proposal == null)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ Proposal not found.", cancellationToken: ct);
            return;
        }

        if (proposal.Status != ProposalStatus.Pending)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ This proposal has already been responded to.", cancellationToken: ct);
            return;
        }

        proposal.Status = ProposalStatus.Rejected;
        proposal.RespondedAt = _clock.UtcNow;

        var deal = proposal.Deal;
        deal.Events.Add(new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            EventType = DealEventType.ProposalRejected,
            ActorUserId = GetUserIdFromQuery(query),
            PayloadJson = $"{{\"proposalId\":\"{proposalId}\"}}",
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        await _botClient.SendMessage(
            query.Message!.Chat.Id,
            "❌ Proposal rejected. You can continue negotiating or cancel the deal.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        var otherPartyTgId = proposal.ProposerUserId == deal.AdvertiserUserId
            ? deal.ChannelOwnerUser.TgUserId
            : deal.AdvertiserUser.TgUserId;

        await _notificationService.SendProposalRejectedAsync(
            otherPartyTgId,
            proposalId,
            deal.Channel.Title);
    }

    private async Task HandleApproveCreativeAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var deal = await _db.Deals
            .Include(d => d.Channel)
            .Include(d => d.ChannelOwnerUser)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal == null)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ Deal not found.", cancellationToken: ct);
            return;
        }

        if (deal.Status != DealStatus.CreativeReview)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ This deal is not awaiting creative review.", cancellationToken: ct);
            return;
        }

        DealStateMachine.ValidateTransition(deal.Status, DealStatus.Scheduled);
        deal.Status = DealStatus.Scheduled;
        deal.UpdatedAt = _clock.UtcNow;

        deal.Events.Add(new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            EventType = DealEventType.CreativeApproved,
            FromStatus = DealStatus.CreativeReview,
            ToStatus = DealStatus.Scheduled,
            ActorUserId = GetUserIdFromQuery(query),
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        await _botClient.SendMessage(
            query.Message!.Chat.Id,
            "✅ Creative approved! The post will be published at the scheduled time.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await _notificationService.SendCreativeApprovedAsync(
            deal.ChannelOwnerUser.TgUserId,
            deal.Id,
            deal.Channel.Title);

        if (deal.ScheduledPostTime.HasValue)
        {
            await _notificationService.SendPostScheduledAsync(
                deal.ChannelOwnerUser.TgUserId,
                deal.Id,
                deal.Channel.Title,
                deal.ScheduledPostTime.Value);
        }
    }

    private async Task HandleRequestEditsAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var deal = await _db.Deals
            .Include(d => d.Channel)
            .Include(d => d.ChannelOwnerUser)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal == null)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ Deal not found.", cancellationToken: ct);
            return;
        }

        if (deal.Status != DealStatus.CreativeReview)
        {
            await _botClient.SendMessage(query.Message!.Chat.Id, "❌ This deal is not awaiting creative review.", cancellationToken: ct);
            return;
        }

        DealStateMachine.ValidateTransition(deal.Status, DealStatus.CreativeDraft);
        deal.Status = DealStatus.CreativeDraft;
        deal.UpdatedAt = _clock.UtcNow;

        deal.Events.Add(new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            EventType = DealEventType.CreativeEditsRequested,
            FromStatus = DealStatus.CreativeReview,
            ToStatus = DealStatus.CreativeDraft,
            ActorUserId = GetUserIdFromQuery(query),
            PayloadJson = "{\"reason\":\"Edits requested by advertiser\"}",
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        await _botClient.SendMessage(
            query.Message!.Chat.Id,
            "✏️ Edit request sent. The channel owner will update the creative.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await _notificationService.SendCreativeEditsRequestedAsync(
            deal.ChannelOwnerUser.TgUserId,
            deal.Id,
            deal.Channel.Title,
            "The advertiser has requested changes to your creative.");
    }

    private Guid? GetUserIdFromQuery(CallbackQuery query)
    {
        var tgUserId = query.From.Id;
        var user = _db.Users.FirstOrDefault(u => u.TgUserId == tgUserId);
        return user?.Id;
    }
}
