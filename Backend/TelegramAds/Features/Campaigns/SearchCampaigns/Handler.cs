using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.SearchCampaigns;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SearchCampaignsResponse> HandleAsync(SearchCampaignsRequest request, CancellationToken ct)
    {
        var query = _db.Campaigns
            .Include(c => c.Applications)
            .AsQueryable();

        if (request.OnlyMine)
        {
            query = query.Where(c => c.AdvertiserUserId == _currentUser.UserId);
        }
        else
        {
            query = query.Where(c => c.Status == CampaignStatus.Open);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Brief.ToLower().Contains(term));
        }

        if (request.MinBudgetTon.HasValue)
        {
            query = query.Where(c => c.BudgetInTon >= request.MinBudgetTon.Value);
        }

        if (request.MaxBudgetTon.HasValue)
        {
            query = query.Where(c => c.BudgetInTon <= request.MaxBudgetTon.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetLanguage))
        {
            query = query.Where(c =>
                c.TargetLanguages == null ||
                c.TargetLanguages.Contains(request.TargetLanguage));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new SearchCampaignItem(
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

        return new SearchCampaignsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
