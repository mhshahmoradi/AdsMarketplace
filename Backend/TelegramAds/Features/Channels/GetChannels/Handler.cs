using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Channels.GetChannels;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetChannelsResponse> HandleAsync(GetChannelsRequest request, CancellationToken ct)
    {
        var query = _db.Channels
            .Include(c => c.Stats)
            .Include(c => c.Admins)
            .AsQueryable();

        if (request.OnlyMine)
        {
            query = query.Where(c =>
                c.OwnerUserId == _currentUser.UserId ||
                c.Admins.Any(a => a.UserId == _currentUser.UserId));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ChannelItem(
                c.Id,
                c.TgChannelId,
                c.Username,
                c.Title,
                c.Description,
                c.Status.ToString(),
                c.Stats != null ? c.Stats.SubscriberCount : null,
                c.Stats != null ? c.Stats.AvgViewsPerPost : null,
                new[] { c.Stats != null ? c.Stats.PrimaryLanguage : null, c.Topic }
                    .Where(t => t != null).Select(t => t!).ToList(),
                c.OwnerUserId == _currentUser.UserId,
                c.Admins.Any(a => a.UserId == _currentUser.UserId),
                c.CreatedAt
            ))
            .ToListAsync(ct);

        return new GetChannelsResponse(items, totalCount);
    }
}
