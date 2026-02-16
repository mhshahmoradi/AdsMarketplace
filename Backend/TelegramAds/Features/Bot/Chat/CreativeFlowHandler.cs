using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramAds.Features.Deals;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Bot.Chat;

public sealed class CreativeFlowHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _db;
    private readonly CreativeSessionStore _creativeSessions;
    private readonly ChatSessionStore _chatSessions;
    private readonly IClock _clock;

    public CreativeFlowHandler(
        ITelegramBotClient botClient,
        AppDbContext db,
        CreativeSessionStore creativeSessions,
        ChatSessionStore chatSessions,
        IClock clock)
    {
        _botClient = botClient;
        _db = db;
        _creativeSessions = creativeSessions;
        _chatSessions = chatSessions;
        _clock = clock;
    }

    public void EnterAwaitingCreativePost(long tgUserId, Guid dealId)
    {
        _chatSessions.RemoveSession(tgUserId);
        _creativeSessions.SetSession(tgUserId, dealId, CreativeUserState.AwaitingCreativePost);
    }

    public bool TryIntercept(long tgUserId, out CreativeSession session)
        => _creativeSessions.TryGetSession(tgUserId, out session);

    public async Task HandleMessageAsync(Message message, CreativeSession session, CancellationToken ct)
    {
        switch (session.State)
        {
            case CreativeUserState.AwaitingCreativePost:
                await HandleCreativePostAsync(message, session, ct);
                break;
            case CreativeUserState.AwaitingRejectionReason:
                await HandleRejectionReasonAsync(message, session, ct);
                break;
        }
    }

    public async Task HandleSelfApproveAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var tgUserId = (long)query.From.Id;

        if (!_creativeSessions.TryGetSession(tgUserId, out var session) || session.DealId != dealId)
        {
            await _botClient.AnswerCallbackQuery(query.Id, "Session expired.", showAlert: true, cancellationToken: ct);
            return;
        }

        var deal = await _db.Deals
            .Include(d => d.AdvertiserUser)
            .Include(d => d.ChannelOwnerUser)
            .Include(d => d.Channel)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null)
        {
            await _botClient.AnswerCallbackQuery(query.Id, "Deal not found.", showAlert: true, cancellationToken: ct);
            _creativeSessions.RemoveSession(tgUserId);
            return;
        }

        var isAdvertiser = deal.AdvertiserUser.TgUserId == tgUserId;
        var peer = isAdvertiser ? deal.ChannelOwnerUser : deal.AdvertiserUser;
        var senderName = isAdvertiser
            ? (deal.AdvertiserUser.FirstName ?? deal.AdvertiserUser.TgUsername ?? "Advertiser")
            : (deal.ChannelOwnerUser.FirstName ?? deal.ChannelOwnerUser.TgUsername ?? "Channel Owner");
        var channelTitle = deal.Channel?.Title ?? "the channel";

        SaveCreativeToDeal(deal, session, query.Message);

        await _db.SaveChangesAsync(ct);

        _chatSessions.RemoveSession(peer.TgUserId);

        var keyboard = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("✅ Approve", $"peer_approve_creative:{dealId}"),
                InlineKeyboardButton.WithCallbackData("❌ Reject", $"peer_reject_creative:{dealId}")
            ]
        ]);

        await _botClient.SendMessage(
            peer.TgUserId,
            $"🎨 <b>Creative Post for Review</b>\n\n" +
            $"From: <b>{senderName}</b>\n" +
            $"Channel: <b>{channelTitle}</b>\n\n" +
            "Please review the post below and approve or reject it.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await CopyMessageToPeer(query.Message!, peer.TgUserId, keyboard, ct);

        _creativeSessions.SetSession(peer.TgUserId, dealId, CreativeUserState.AwaitingPeerReview);

        await DeletePreviewMessages(query.Message!.Chat.Id, session, ct);

        _creativeSessions.SetSession(tgUserId, dealId, CreativeUserState.AwaitingPeerReview);

        await _botClient.SendMessage(
            query.Message!.Chat.Id,
            "✅ Your creative has been sent to the other party for review.\n\nPlease wait for their response.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public async Task HandleSelfRejectAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var tgUserId = (long)query.From.Id;
        var chatId = query.Message!.Chat.Id;

        if (_creativeSessions.TryGetSession(tgUserId, out var session))
            await DeletePreviewMessages(chatId, session, ct);

        try
        {
            await _botClient.DeleteMessage(chatId, query.Message.MessageId, cancellationToken: ct);
        }
        catch
        {
        }

        _creativeSessions.SetSession(tgUserId, dealId, CreativeUserState.AwaitingCreativePost);

        await _botClient.SendMessage(
            chatId,
            "🔄 Post discarded. Please send a new post for this deal.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public async Task HandlePeerApproveAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var tgUserId = (long)query.From.Id;

        var deal = await _db.Deals
            .Include(d => d.AdvertiserUser)
            .Include(d => d.ChannelOwnerUser)
            .Include(d => d.Channel)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null)
        {
            await _botClient.AnswerCallbackQuery(query.Id, "Deal not found.", showAlert: true, cancellationToken: ct);
            _creativeSessions.RemoveSession(tgUserId);
            return;
        }

        if (deal.Status is not (DealStatus.Paid or DealStatus.CreativeDraft or DealStatus.CreativeReview))
        {
            await _botClient.AnswerCallbackQuery(query.Id, "Deal is not in a valid state.", showAlert: true, cancellationToken: ct);
            return;
        }

        var previousStatus = deal.Status;
        deal.Status = DealStatus.Scheduled;
        deal.UpdatedAt = _clock.UtcNow;

        _db.DealEvents.Add(new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            EventType = DealEventType.CreativeApproved,
            FromStatus = previousStatus,
            ToStatus = DealStatus.Scheduled,
            ActorUserId = GetUserId(tgUserId),
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _creativeSessions.RemoveSession(tgUserId);

        var isAdvertiser = deal.AdvertiserUser.TgUserId == tgUserId;
        var submitter = isAdvertiser ? deal.ChannelOwnerUser : deal.AdvertiserUser;

        _creativeSessions.RemoveSession(submitter.TgUserId);

        var channelTitle = deal.Channel?.Title ?? "the channel";

        try
        {
            await _botClient.DeleteMessage(query.Message!.Chat.Id, query.Message.MessageId, cancellationToken: ct);
        }
        catch
        {
        }

        await _botClient.SendMessage(
            query.Message!.Chat.Id,
            $"✅ Creative approved! The post for <b>{channelTitle}</b> will be published at the scheduled time.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await _botClient.SendMessage(
            submitter.TgUserId,
            $"🎉 <b>Creative Approved!</b>\n\n" +
            $"Your creative for <b>{channelTitle}</b> has been approved.\n" +
            (deal.ScheduledPostTime.HasValue
                ? $"It will be published at {deal.ScheduledPostTime.Value:g} UTC."
                : "It will be published shortly."),
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public async Task HandlePeerRejectAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var tgUserId = (long)query.From.Id;

        var deal = await _db.Deals
            .Include(d => d.Channel)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null)
        {
            await _botClient.AnswerCallbackQuery(query.Id, "Deal not found.", showAlert: true, cancellationToken: ct);
            _creativeSessions.RemoveSession(tgUserId);
            return;
        }

        try
        {
            await _botClient.DeleteMessage(query.Message!.Chat.Id, query.Message.MessageId, cancellationToken: ct);
        }
        catch
        {
        }

        _chatSessions.RemoveSession(tgUserId);
        _creativeSessions.SetSession(tgUserId, dealId, CreativeUserState.AwaitingRejectionReason);

        var channelTitle = deal.Channel?.Title ?? "the channel";

        await _botClient.SendMessage(
            query.Message!.Chat.Id,
            $"❌ You are rejecting the creative for <b>{channelTitle}</b>.\n\n" +
            "Please type the reason for rejection:",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task HandleCreativePostAsync(Message message, CreativeSession session, CancellationToken ct)
    {
        var tgUserId = (long)message.From!.Id;
        var chatId = message.Chat.Id;

        var keyboard = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("✅ Approve & Send", $"self_approve_creative:{session.DealId}"),
                InlineKeyboardButton.WithCallbackData("❌ Discard", $"self_reject_creative:{session.DealId}")
            ]
        ]);

        var sent = await CopyMessageToSelf(message, chatId, keyboard, ct);

        var creativeText = message.Text ?? message.Caption;
        var mediaItem = ExtractMediaItem(message);

        _creativeSessions.SetSession(tgUserId, new CreativeSession(
            session.DealId,
            CreativeUserState.AwaitingCreativePost,
            sent,
            creativeText,
            mediaItem));

        await _botClient.SendMessage(
            chatId,
            "👆 Review your post above. Tap <b>Approve & Send</b> to forward it, or <b>Discard</b> to try again.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task HandleRejectionReasonAsync(Message message, CreativeSession session, CancellationToken ct)
    {
        var tgUserId = (long)message.From!.Id;
        var reason = message.Text;

        if (string.IsNullOrWhiteSpace(reason))
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "Please type a text reason for the rejection.",
                cancellationToken: ct);
            return;
        }

        var deal = await _db.Deals
            .Include(d => d.AdvertiserUser)
            .Include(d => d.ChannelOwnerUser)
            .Include(d => d.Channel)
            .FirstOrDefaultAsync(d => d.Id == session.DealId, ct);

        if (deal is null)
        {
            _creativeSessions.RemoveSession(tgUserId);
            await _botClient.SendMessage(message.Chat.Id, "❌ Deal not found.", cancellationToken: ct);
            return;
        }

        var previousStatus = deal.Status;

        if (deal.Status is DealStatus.Paid or DealStatus.CreativeDraft or DealStatus.CreativeReview)
        {
            deal.Status = DealStatus.CreativeDraft;
        }

        deal.CreativeRejectionReason = reason;
        deal.UpdatedAt = _clock.UtcNow;

        _db.DealEvents.Add(new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            EventType = DealEventType.CreativeRejected,
            FromStatus = previousStatus,
            ToStatus = DealStatus.CreativeDraft,
            ActorUserId = GetUserId(tgUserId),
            PayloadJson = $"{{\"reason\":\"{EscapeJson(reason)}\"}}",
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var isAdvertiser = deal.AdvertiserUser.TgUserId == tgUserId;
        var submitter = isAdvertiser ? deal.ChannelOwnerUser : deal.AdvertiserUser;
        var channelTitle = deal.Channel?.Title ?? "the channel";

        _creativeSessions.RemoveSession(tgUserId);

        _chatSessions.RemoveSession(submitter.TgUserId);
        _creativeSessions.SetSession(submitter.TgUserId, deal.Id, CreativeUserState.AwaitingCreativePost);

        await _botClient.SendMessage(
            message.Chat.Id,
            "✅ Rejection reason sent. Waiting for a new creative post.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await _botClient.SendMessage(
            submitter.TgUserId,
            $"❌ <b>Creative Rejected</b>\n\n" +
            $"Channel: <b>{channelTitle}</b>\n\n" +
            $"Reason: <i>{reason}</i>\n\n" +
            "Please send a new post for this deal.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private void SaveCreativeToDeal(Deal deal, CreativeSession session, Message? queryMessage)
    {
        if (session.MediaItem is not null)
        {
            deal.CreativeText = session.CreativeText;
            deal.CreativeMediaJson = JsonSerializer.Serialize(new[] { session.MediaItem }, JsonOptions);
        }
        else if (queryMessage is not null)
        {
            deal.CreativeText = queryMessage.Text ?? queryMessage.Caption;
            var item = ExtractMediaItem(queryMessage);
            deal.CreativeMediaJson = item is not null
                ? JsonSerializer.Serialize(new[] { item }, JsonOptions)
                : null;
        }
        else
        {
            deal.CreativeText = session.CreativeText;
        }

        deal.UpdatedAt = _clock.UtcNow;
    }

    private async Task DeletePreviewMessages(long chatId, CreativeSession session, CancellationToken ct)
    {
        if (session.PreviewMessageId is not null)
        {
            try { await _botClient.DeleteMessage(chatId, session.PreviewMessageId.Value, cancellationToken: ct); }
            catch { }
        }
    }

    private static CreativeMediaItem? ExtractMediaItem(Message message)
    {
        if (message.Photo is { Length: > 0 })
            return new CreativeMediaItem("photo", message.Photo[^1].FileId);
        if (message.Video is not null)
            return new CreativeMediaItem("video", message.Video.FileId);
        if (message.Document is not null)
            return new CreativeMediaItem("document", message.Document.FileId);
        return null;
    }

    private async Task<int> CopyMessageToSelf(Message message, long chatId, InlineKeyboardMarkup keyboard, CancellationToken ct)
    {
        if (message.Photo is { Length: > 0 })
        {
            var photo = message.Photo[^1];
            var sent = await _botClient.SendPhoto(
                chatId,
                InputFile.FromFileId(photo.FileId),
                caption: message.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return sent.MessageId;
        }

        if (message.Video is not null)
        {
            var sent = await _botClient.SendVideo(
                chatId,
                InputFile.FromFileId(message.Video.FileId),
                caption: message.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return sent.MessageId;
        }

        if (message.Document is not null)
        {
            var sent = await _botClient.SendDocument(
                chatId,
                InputFile.FromFileId(message.Document.FileId),
                caption: message.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return sent.MessageId;
        }

        if (message.Voice is not null)
        {
            var sent = await _botClient.SendVoice(
                chatId,
                InputFile.FromFileId(message.Voice.FileId),
                caption: message.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return sent.MessageId;
        }

        if (message.Text is not null)
        {
            var sent = await _botClient.SendMessage(
                chatId,
                message.Text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return sent.MessageId;
        }

        var copied = await _botClient.CopyMessage(
            chatId,
            message.Chat.Id,
            message.MessageId,
            replyMarkup: keyboard,
            cancellationToken: ct);
        return copied.Id;
    }

    private async Task CopyMessageToPeer(Message sourceMsg, long peerChatId, InlineKeyboardMarkup keyboard, CancellationToken ct)
    {
        if (sourceMsg.Photo is { Length: > 0 })
        {
            var photo = sourceMsg.Photo[^1];
            await _botClient.SendPhoto(
                peerChatId,
                InputFile.FromFileId(photo.FileId),
                caption: sourceMsg.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return;
        }

        if (sourceMsg.Video is not null)
        {
            await _botClient.SendVideo(
                peerChatId,
                InputFile.FromFileId(sourceMsg.Video.FileId),
                caption: sourceMsg.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return;
        }

        if (sourceMsg.Document is not null)
        {
            await _botClient.SendDocument(
                peerChatId,
                InputFile.FromFileId(sourceMsg.Document.FileId),
                caption: sourceMsg.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return;
        }

        if (sourceMsg.Voice is not null)
        {
            await _botClient.SendVoice(
                peerChatId,
                InputFile.FromFileId(sourceMsg.Voice.FileId),
                caption: sourceMsg.Caption,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return;
        }

        if (sourceMsg.Text is not null)
        {
            await _botClient.SendMessage(
                peerChatId,
                sourceMsg.Text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
            return;
        }

        await _botClient.CopyMessage(
            peerChatId,
            sourceMsg.Chat.Id,
            sourceMsg.MessageId,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private Guid? GetUserId(long tgUserId)
    {
        var user = _db.Users.FirstOrDefault(u => u.TgUserId == tgUserId);
        return user?.Id;
    }

    private static string EscapeJson(string text)
        => text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
