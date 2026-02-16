using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace TelegramAds.Features.Bot.Chat;

public enum CreativeUserState
{
    None,
    AwaitingCreativePost,
    AwaitingPeerReview,
    AwaitingRejectionReason
}

public sealed record CreativeMediaItem(string Type, string FileId);

public sealed record CreativeSession(
    Guid DealId,
    CreativeUserState State,
    int? PreviewMessageId = null,
    string? CreativeText = null,
    CreativeMediaItem? MediaItem = null);

public sealed class CreativeSessionStore
{
    private readonly ConcurrentDictionary<long, CreativeSession> _sessions = new();

    public bool TryGetSession(long tgUserId, out CreativeSession session)
        => _sessions.TryGetValue(tgUserId, out session!);

    public void SetSession(long tgUserId, Guid dealId, CreativeUserState state, int? previewMessageId = null)
        => _sessions[tgUserId] = new CreativeSession(dealId, state, previewMessageId, null, null);

    public void SetSession(long tgUserId, CreativeSession session)
        => _sessions[tgUserId] = session;

    public bool RemoveSession(long tgUserId)
        => _sessions.TryRemove(tgUserId, out _);
}


