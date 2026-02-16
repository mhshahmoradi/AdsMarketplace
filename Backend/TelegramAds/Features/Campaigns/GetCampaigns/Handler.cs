using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.GetCampaigns;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetCampaignsResponse> HandleAsync(GetCampaignsRequest request, CancellationToken ct)
    {
        var query = _db.Campaigns
            .Include(c => c.Applications)
            .AsQueryable();

        if (request.OnlyMine)
        {
            query = query.Where(c => c.AdvertiserUserId == _currentUser.UserId);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CampaignItem(
                c.Id,
                c.Title,
                c.Brief,
                c.BudgetInTon,
                c.MinSubscribers,
                c.MinAvgViews,
                c.TargetLanguages,
                c.ScheduleStart,
                c.ScheduleEnd,
                c.Status.ToString(),
                c.Applications.Count,
                c.AdvertiserUserId == _currentUser.UserId,
                c.CreatedAt
            ))
            .ToListAsync(ct);

        return new GetCampaignsResponse(items, totalCount);
    }
}
