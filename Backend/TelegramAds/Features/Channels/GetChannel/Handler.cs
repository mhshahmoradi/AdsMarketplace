using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;

namespace TelegramAds.Features.Channels.GetChannel;

public sealed class Handler
{
    private readonly AppDbContext _db;

    public Handler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetChannelResponse> HandleAsync(GetChannelRequest request, CancellationToken ct)
    {
        var channel = await _db.Channels
            .Include(c => c.Stats)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);

        if (channel is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Channel not found", 404);
        }

        ChannelStatsDto? statsDto = null;
        if (channel.Stats is not null)
        {
            statsDto = new ChannelStatsDto(
                channel.Stats.SubscriberCount,
                channel.Stats.AvgViewsPerPost,
                channel.Stats.AvgSharesPerPost,
                channel.Stats.AvgReactionsPerPost,
                channel.Stats.AvgViewsPerStory,
                channel.Stats.AvgSharesPerStory,
                channel.Stats.AvgReactionsPerStory,
                channel.Stats.EnabledNotificationsPercent,
                channel.Stats.FollowersCount,
                channel.Stats.FollowersPrevCount,
                channel.Stats.PrimaryLanguage,
                channel.Stats.LanguagesGraphJson,
                channel.Stats.PremiumSubscriberCount,
                channel.Stats.StatsStartDate,
                channel.Stats.StatsEndDate,
                channel.Stats.FetchedAt
            );
        }

        var tags = new List<string>();
        if (!string.IsNullOrEmpty(channel.Stats?.PrimaryLanguage))
            tags.Add(channel.Stats.PrimaryLanguage);
        if (!string.IsNullOrEmpty(channel.Topic))
            tags.Add(channel.Topic);

        return new GetChannelResponse(
            channel.Id,
            channel.TgChannelId,
            channel.Username,
            channel.Title,
            channel.Description,
            channel.PhotoUrl,
            channel.Status.ToString(),
            statsDto,
            tags,
            channel.CreatedAt
        );
    }
}
