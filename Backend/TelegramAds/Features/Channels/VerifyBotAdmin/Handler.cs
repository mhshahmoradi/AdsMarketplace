using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Telegram;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Channels.VerifyBotAdmin;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly ITelegramBotClient _botClient;
    private readonly WTelegramService _wtelegram;
    private readonly IClock _clock;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, ITelegramBotClient botClient, WTelegramService wtelegram, IClock clock, CurrentUser currentUser)
    {
        _db = db;
        _botClient = botClient;
        _wtelegram = wtelegram;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<VerifyBotAdminResponse> HandleAsync(VerifyBotAdminRequest request, CancellationToken ct)
    {
        var channel = await GetChannelOrThrowAsync(request.ChannelId, ct);
        await EnsureCallerAuthorizedAsync(channel, ct);

        try
        {
            var chatId = channel.TgChannelId;

            if (!await IsBotAdminAsync(chatId, ct))
                return FailureResponse(channel, isBotAdmin: false, isUserAdmin: false,
                    "Bot is not an administrator of this channel. Please add the bot as admin with posting permissions.");

            if (!await IsUserAdminAsync(chatId, request.RealUserTgId, ct))
                return FailureResponse(channel, isBotAdmin: true, isUserAdmin: false,
                    $"The Telegram user (ID: {request.RealUserTgId}) is not an administrator of this channel.");

            await VerifyCallerTgAdminAsync(chatId, ct);

            channel.Status = ChannelStatus.Verified;
            channel.UpdatedAt = _clock.UtcNow;

            await TrySyncChannelStatsAsync(channel);

            await _db.SaveChangesAsync(ct);

            return new VerifyBotAdminResponse(
                channel.Id, true, true, true,
                channel.Status.ToString(),
                "Channel verified successfully. Both bot and user are admins.",
                channel.Stats?.PremiumSubscriberCount
            );
        }
        catch (AppException) { throw; }
        catch (Exception ex)
        {
            throw new AppException(ErrorCodes.TelegramError, $"Failed to verify admin status: {ex.Message}");
        }
    }

    private async Task<Channel> GetChannelOrThrowAsync(Guid channelId, CancellationToken ct)
    {
        var channel = await _db.Channels
            .Include(c => c.Stats)
            .FirstOrDefaultAsync(c => c.Id == channelId, ct);

        return channel ?? throw new AppException(ErrorCodes.NotFound, "Channel not found", 404);
    }

    private async Task EnsureCallerAuthorizedAsync(Channel channel, CancellationToken ct)
    {
        if (channel.OwnerUserId == _currentUser.UserId)
            return;

        var isAdmin = await _db.ChannelAdmins
            .AnyAsync(a => a.ChannelId == channel.Id && a.UserId == _currentUser.UserId && a.CanManageListings, ct);

        if (!isAdmin)
            throw new AppException(ErrorCodes.Forbidden, "You are not authorized to verify this channel", 403);
    }

    private async Task<bool> IsBotAdminAsync(long chatId, CancellationToken ct)
    {
        var botMe = await _botClient.GetMe(ct);
        try
        {
            var botMember = await _botClient.GetChatMember(chatId, botMe.Id, ct);
            return botMember.Status == ChatMemberStatus.Administrator;
        }
        catch { return false; }
    }

    private async Task<bool> IsUserAdminAsync(long chatId, long userTgId, CancellationToken ct)
    {
        try
        {
            var member = await _botClient.GetChatMember(chatId, userTgId, ct);
            return member.Status is ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
        }
        catch { return false; }
    }

    private async Task VerifyCallerTgAdminAsync(long chatId, CancellationToken ct)
    {
        if (_currentUser.TgUserId <= 0)
            return;

        try
        {
            var callerMember = await _botClient.GetChatMember(chatId, _currentUser.TgUserId, ct);
            if (callerMember.Status is not (ChatMemberStatus.Administrator or ChatMemberStatus.Creator))
                throw new AppException(ErrorCodes.UserNotAdmin, "You are not an administrator of this channel in Telegram", 403);
        }
        catch (AppException) { throw; }
        catch { }
    }

    private async Task TrySyncChannelStatsAsync(Channel channel)
    {
        try
        {
            await SyncBroadcastStatsAsync(channel);
            await SyncBoostStatusAsync(channel);
        }
        catch { }
    }

    private async Task SyncBroadcastStatsAsync(Channel channel)
    {
        var broadcastStats = await _wtelegram.GetChannelStatsAsync(channel.Username);
        if (broadcastStats is null)
            return;

        var stats = channel.Stats ?? new ChannelStats
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id
        };

        stats.SubscriberCount = (int)broadcastStats.Followers.Current;
        stats.FollowersCount = (int)broadcastStats.Followers.Current;
        stats.FollowersPrevCount = (int)broadcastStats.Followers.Previous;
        stats.AvgViewsPerPost = broadcastStats.ViewsPerPost.Current;
        stats.AvgSharesPerPost = broadcastStats.SharesPerPost.Current;
        stats.AvgReactionsPerPost = broadcastStats.ReactionsPerPost.Current;
        stats.AvgViewsPerStory = broadcastStats.ViewsPerStory.Current;
        stats.AvgSharesPerStory = broadcastStats.SharesPerStory.Current;
        stats.AvgReactionsPerStory = broadcastStats.ReactionsPerStory.Current;
        stats.EnabledNotificationsPercent = broadcastStats.EnabledNotificationsPercent;
        stats.PrimaryLanguage = broadcastStats.TopLanguage;
        stats.LanguagesGraphJson = broadcastStats.LanguagesGraphJson;
        stats.StatsStartDate = broadcastStats.PeriodStart;
        stats.StatsEndDate = broadcastStats.PeriodEnd;
        stats.FetchedAt = _clock.UtcNow;

        channel.Stats = stats;
    }

    private async Task SyncBoostStatusAsync(Channel channel)
    {
        var boostStatus = await _wtelegram.GetBoostStatusAsync(channel.Username);
        if (boostStatus is not null && channel.Stats is not null)
            channel.Stats.PremiumSubscriberCount = boostStatus.PremiumAudiencePercent;
    }

    private static VerifyBotAdminResponse FailureResponse(Channel channel, bool isBotAdmin, bool isUserAdmin, string message) =>
        new(channel.Id, false, isBotAdmin, isUserAdmin, channel.Status.ToString(), message);
}
