using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Bot.Chat;

public sealed class ChatHandler
{
    private static readonly DealStatus[] ActiveDealStatuses =
    [
        DealStatus.Agreed,
        DealStatus.AwaitingPayment,
        DealStatus.Paid,
        DealStatus.CreativeDraft,
        DealStatus.CreativeReview,
        DealStatus.Scheduled,
        DealStatus.Posted
    ];

    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _db;
    private readonly ChatSessionStore _sessions;
    private readonly IClock _clock;

    public ChatHandler(
        ITelegramBotClient botClient,
        AppDbContext db,
        ChatSessionStore sessions,
        IClock clock)
    {
        _botClient = botClient;
        _db = db;
        _sessions = sessions;
        _clock = clock;
    }

    public async Task HandleChatCommandAsync(Message message, CancellationToken ct)
    {
        var tgUserId = message.From!.Id;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TgUserId == (long)tgUserId, ct);
        if (user is null)
        {
            await _botClient.SendMessage(message.Chat.Id,
                "❌ You are not registered. Please use the Mini App first.",
                cancellationToken: ct);
            return;
        }

        var deals = await _db.Deals
            .Include(d => d.AdvertiserUser)
            .Include(d => d.ChannelOwnerUser)
            .Include(d => d.Channel)
            .Where(d => ActiveDealStatuses.Contains(d.Status))
            .Where(d => d.AdvertiserUserId == user.Id || d.ChannelOwnerUserId == user.Id)
            .ToListAsync(ct);

        if (deals.Count == 0)
        {
            await _botClient.SendMessage(message.Chat.Id,
                "📭 You have no active deals to chat about.",
                cancellationToken: ct);
            return;
        }

        var grouped = deals
            .GroupBy(d => d.AdvertiserUserId == user.Id ? d.ChannelOwnerUserId : d.AdvertiserUserId)
            .ToList();

        var buttons = new List<IEnumerable<InlineKeyboardButton>>();
        foreach (var group in grouped)
        {
            var peer = group.First().AdvertiserUserId == user.Id
                ? group.First().ChannelOwnerUser
                : group.First().AdvertiserUser;

            var dealId = group.First().Id;
            var channelTitle = group.First().Channel.Title;
            var displayName = peer.FirstName ?? peer.TgUsername ?? "User";
            var label = $"💬 {displayName} — {channelTitle}";

            buttons.Add([InlineKeyboardButton.WithCallbackData(label, $"chat_select:{dealId}")]);
        }

        await _botClient.SendMessage(message.Chat.Id,
            "👥 <b>Select a deal partner to chat with:</b>",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    public async Task HandleChatSelectAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var tgUserId = query.From.Id;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TgUserId == (long)tgUserId, ct);
        if (user is null) return;

        var deal = await _db.Deals
            .Include(d => d.AdvertiserUser)
            .Include(d => d.ChannelOwnerUser)
            .Include(d => d.Channel)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null || !ActiveDealStatuses.Contains(deal.Status))
        {
            await _botClient.SendMessage(query.Message!.Chat.Id,
                "❌ Deal not found or no longer active.",
                cancellationToken: ct);
            return;
        }

        var isAdvertiser = deal.AdvertiserUserId == user.Id;
        var peer = isAdvertiser ? deal.ChannelOwnerUser : deal.AdvertiserUser;

        _sessions.SetSession((long)tgUserId, peer.TgUserId, deal.Id);

        var peerName = peer.FirstName ?? peer.TgUsername ?? "User";
        await _botClient.SendMessage(query.Message!.Chat.Id,
            $"💬 Chat started with <b>{peerName}</b> (Deal: {deal.Channel.Title})\n\n" +
            "Send your messages and they will be forwarded.\n" +
            "Type /quit to end the chat.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public async Task HandleStartChatAsync(CallbackQuery query, Guid dealId, CancellationToken ct)
    {
        var tgUserId = query.From.Id;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TgUserId == (long)tgUserId, ct);
        if (user is null) return;

        var deal = await _db.Deals
            .Include(d => d.AdvertiserUser)
            .Include(d => d.ChannelOwnerUser)
            .Include(d => d.Channel)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null || !ActiveDealStatuses.Contains(deal.Status))
        {
            await _botClient.SendMessage(query.Message!.Chat.Id,
                "❌ Deal not found or no longer active.",
                cancellationToken: ct);
            return;
        }

        var isAdvertiser = deal.AdvertiserUserId == user.Id;
        var peer = isAdvertiser ? deal.ChannelOwnerUser : deal.AdvertiserUser;

        _sessions.SetSession((long)tgUserId, peer.TgUserId, deal.Id);

        var peerName = peer.FirstName ?? peer.TgUsername ?? "User";
        await _botClient.SendMessage(query.Message!.Chat.Id,
            $"💬 Chat started with <b>{peerName}</b> (Deal: {deal.Channel.Title})\n\n" +
            "Send your messages and they will be forwarded.\n" +
            "Type /quit to end the chat.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public async Task HandleQuitAsync(Message message, CancellationToken ct)
    {
        var tgUserId = message.From!.Id;
        if (_sessions.RemoveSession((long)tgUserId))
        {
            await _botClient.SendMessage(message.Chat.Id,
                "👋 Chat ended. Use /chat to start a new conversation.",
                cancellationToken: ct);
        }
        else
        {
            await _botClient.SendMessage(message.Chat.Id,
                "ℹ️ You are not in a chat session.",
                cancellationToken: ct);
        }
    }

    public async Task<bool> TryForwardMessageAsync(Message message, CancellationToken ct)
    {
        var tgUserId = message.From!.Id;
        if (!_sessions.TryGetSession((long)tgUserId, out var session))
            return false;

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.TgUserId == (long)tgUserId, ct);
        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.TgUserId == session.PeerTgUserId, ct);
        if (sender is null || receiver is null)
            return false;

        var deal = await _db.Deals
            .Include(d => d.Channel)
            .FirstOrDefaultAsync(d => d.Id == session.DealId, ct);

        if (deal is null || !ActiveDealStatuses.Contains(deal.Status))
        {
            _sessions.RemoveSession((long)tgUserId);
            await _botClient.SendMessage(message.Chat.Id,
                "❌ The deal is no longer active. Chat ended.",
                cancellationToken: ct);
            return true;
        }

        var senderName = sender.FirstName ?? sender.TgUsername ?? "User";
        var peerHasSession = _sessions.TryGetSession(session.PeerTgUserId, out var peerSession)
                             && peerSession.PeerTgUserId == (long)tgUserId;

        InlineKeyboardMarkup? replyMarkup = peerHasSession
            ? null
            : new InlineKeyboardMarkup(new[]
                { InlineKeyboardButton.WithCallbackData("💬 Start conversation", $"chat_start:{deal.Id}") });

        var header = $"📩 <b>{senderName}</b> ({deal.Channel.Title}):";
        long? forwardedMsgId = null;

        if (message.Text is not null)
        {
            var sent = await _botClient.SendMessage(session.PeerTgUserId,
                $"{header}\n\n{message.Text}",
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            forwardedMsgId = sent.MessageId;
        }
        else if (message.Photo is { Length: > 0 })
        {
            var photo = message.Photo[^1];
            var sent = await _botClient.SendPhoto(session.PeerTgUserId,
                InputFile.FromFileId(photo.FileId),
                caption: $"{header}\n\n{message.Caption ?? ""}",
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            forwardedMsgId = sent.MessageId;
        }
        else if (message.Document is not null)
        {
            var sent = await _botClient.SendDocument(session.PeerTgUserId,
                InputFile.FromFileId(message.Document.FileId),
                caption: $"{header}\n\n{message.Caption ?? ""}",
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            forwardedMsgId = sent.MessageId;
        }
        else if (message.Video is not null)
        {
            var sent = await _botClient.SendVideo(session.PeerTgUserId,
                InputFile.FromFileId(message.Video.FileId),
                caption: $"{header}\n\n{message.Caption ?? ""}",
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            forwardedMsgId = sent.MessageId;
        }
        else if (message.Voice is not null)
        {
            var sent = await _botClient.SendVoice(session.PeerTgUserId,
                InputFile.FromFileId(message.Voice.FileId),
                caption: header,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            forwardedMsgId = sent.MessageId;
        }
        else if (message.Sticker is not null)
        {
            await _botClient.SendMessage(session.PeerTgUserId,
                header,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            var sent = await _botClient.SendSticker(session.PeerTgUserId,
                InputFile.FromFileId(message.Sticker.FileId),
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            forwardedMsgId = sent.MessageId;
        }
        else
        {
            var sent = await _botClient.CopyMessage(session.PeerTgUserId,
                message.Chat.Id,
                message.MessageId,
                replyMarkup: replyMarkup,
                cancellationToken: ct);
            forwardedMsgId = sent.Id;
        }

        _db.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            DealId = session.DealId,
            SenderUserId = sender.Id,
            ReceiverUserId = receiver.Id,
            TgMessageId = message.MessageId,
            ForwardedTgMessageId = forwardedMsgId,
            Text = message.Text ?? message.Caption,
            HasMedia = message.Photo is not null || message.Document is not null
                       || message.Video is not null || message.Voice is not null
                       || message.Sticker is not null,
            SentAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
