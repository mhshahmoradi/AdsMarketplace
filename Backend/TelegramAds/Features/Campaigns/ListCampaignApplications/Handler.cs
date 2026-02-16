using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.ListCampaignApplications;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ListCampaignApplicationsResponse> HandleAsync(ListCampaignApplicationsRequest request, CancellationToken ct)
    {
        var query = _db.CampaignApplications
            .Include(a => a.Campaign)
            .Include(a => a.Channel)
            .ThenInclude(c => c.Stats)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Role))
        {
            if (request.Role.Equals("campaign-owner", StringComparison.OrdinalIgnoreCase) ||
                request.Role.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
                request.Role.Equals("received", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Campaign.AdvertiserUserId == _currentUser.UserId);
            }
            else if (request.Role.Equals("applicant", StringComparison.OrdinalIgnoreCase) ||
                     request.Role.Equals("sent", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Channel.OwnerUserId == _currentUser.UserId);
            }
        }
        else
        {
            query = query.Where(a =>
                a.Campaign.AdvertiserUserId == _currentUser.UserId ||
                a.Channel.OwnerUserId == _currentUser.UserId);
        }

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<ApplicationStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(a =>
                a.Channel.Username.ToLower().Contains(search) ||
                a.Channel.Title.ToLower().Contains(search) ||
                a.Campaign.Title.ToLower().Contains(search) ||
                a.Campaign.Brief.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new CampaignApplicationSummaryDto(
                a.Id,
                a.Status.ToString(),
                a.ProposedPriceInTon,
                a.Message,
                a.CreatedAt,
                new ApplicationChannelDto(
                    a.ChannelId,
                    a.Channel.Username,
                    a.Channel.Title,
                    a.Channel.Stats != null ? a.Channel.Stats.SubscriberCount : 0,
                    a.Channel.Stats != null ? a.Channel.Stats.AvgViewsPerPost : 0
                ),
                new CampaignAppCampaignDto(
                    a.CampaignId,
                    a.Campaign.Title,
                    a.Campaign.Brief,
                    a.Campaign.BudgetInTon
                )
            ))
            .ToListAsync(ct);

        return new ListCampaignApplicationsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
