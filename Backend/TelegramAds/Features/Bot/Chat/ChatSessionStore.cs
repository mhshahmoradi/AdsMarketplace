using System.Collections.Concurrent;

namespace TelegramAds.Features.Bot.Chat;

public sealed class ChatSessionStore
{
    private readonly ConcurrentDictionary<long, ActiveChat> _sessions = new();

    public bool TryGetSession(long tgUserId, out ActiveChat session)
        => _sessions.TryGetValue(tgUserId, out session!);

    public void SetSession(long tgUserId, long peerTgUserId, Guid dealId)
        => _sessions[tgUserId] = new ActiveChat(peerTgUserId, dealId);

    public bool RemoveSession(long tgUserId)
        => _sessions.TryRemove(tgUserId, out _);
}

public sealed record ActiveChat(long PeerTgUserId, Guid DealId);
