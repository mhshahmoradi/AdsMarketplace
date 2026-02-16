using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Deals.ListDeals;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ListDealsResponse> HandleAsync(ListDealsRequest request, CancellationToken ct)
    {
        var query = _db.Deals
            .Include(d => d.Campaign)
            .Include(d => d.Channel)
                .ThenInclude(c => c.Stats)
            .Include(d => d.Listing)
                .ThenInclude(l => l.Channel)
                    .ThenInclude(c => c.Stats)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Role))
        {
            if (request.Role.Equals("advertiser", StringComparison.OrdinalIgnoreCase) ||
                request.Role.Equals("sent", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.AdvertiserUserId == _currentUser.UserId);
            }
            else if (request.Role.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
                     request.Role.Equals("received", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.ChannelOwnerUserId == _currentUser.UserId);
            }
        }
        else
        {
            query = query.Where(d =>
                d.AdvertiserUserId == _currentUser.UserId ||
                d.ChannelOwnerUserId == _currentUser.UserId);
        }

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<DealStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(d => d.Status == status);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(d =>
                (d.Listing != null && d.Listing.Title.ToLower().Contains(search)) ||
                (d.Campaign != null && d.Campaign.Title.ToLower().Contains(search)) ||
                (d.Campaign != null && d.Campaign.Brief.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DealSummaryDto(
                d.Id,
                d.ListingApplicationId.HasValue ? d.ListingId : null,
                d.CampaignApplicationId.HasValue ? d.ChannelId : null,
                d.CampaignId,
                d.AdvertiserUserId,
                d.ChannelOwnerUserId,
                d.Status.ToString(),
                d.AgreedPriceInTon,
                d.AdFormat.ToString(),
                d.ScheduledPostTime,
                d.EscrowAddress,
                d.PostedAt,
                d.VerifiedAt,
                d.CreatedAt,
                d.UpdatedAt,
                d.ExpiresAt,
                d.Campaign != null
                    ? new ListDealCampaignDto(
                        d.CampaignId,
                        d.Campaign.Title,
                        d.Campaign.Brief,
                        d.Campaign.BudgetInTon,
                        d.Campaign.Status.ToString(),
                        d.Campaign.ScheduleStart,
                        d.Campaign.ScheduleEnd
                    )
                    : null,
                d.Listing != null
                    ? new ListDealListingDto(
                        d.ListingId,
                        d.Listing.Title,
                        d.Listing.Description,
                        d.Listing.Status.ToString(),
                        new ListDealChannelDto(
                            d.Listing.Channel.Id,
                            d.Listing.Channel.Username,
                            d.Listing.Channel.Title,
                            d.Listing.Channel.Description,
                            d.Listing.Channel.Status.ToString(),
                            d.Listing.Channel.Stats != null
                                ? new ListDealChannelStatsDto(
                                    d.Listing.Channel.Stats.SubscriberCount,
                                    d.Listing.Channel.Stats.AvgViewsPerPost,
                                    d.Listing.Channel.Stats.AvgReachPerPost,
                                    d.Listing.Channel.Stats.FetchedAt
                                )
                                : null
                        )
                    )
                    : d.Channel != null
                        ? new ListDealListingDto(
                            null,
                            d.Channel.Title,
                            d.Channel.Description,
                            d.Channel.Status.ToString(),
                            new ListDealChannelDto(
                                d.Channel.Id,
                                d.Channel.Username,
                                d.Channel.Title,
                                d.Channel.Description,
                                d.Channel.Status.ToString(),
                                d.Channel.Stats != null
                                    ? new ListDealChannelStatsDto(
                                        d.Channel.Stats.SubscriberCount,
                                        d.Channel.Stats.AvgViewsPerPost,
                                        d.Channel.Stats.AvgReachPerPost,
                                        d.Channel.Stats.FetchedAt
                                    )
                                    : null
                            )
                        )
                        : null
            ))
            .ToListAsync(ct);

        return new ListDealsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
