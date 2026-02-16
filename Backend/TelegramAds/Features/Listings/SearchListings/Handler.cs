using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Listings.SearchListings;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SearchListingsResponse> HandleAsync(SearchListingsRequest request, CancellationToken ct)
    {
        var query = BuildBaseQuery(request.OnlyMine);
        query = ApplyFilters(query, request);

        var totalCount = await query.CountAsync(ct);
        var items = await ExecuteQueryAsync(query, request, ct);

        return new SearchListingsResponse(items, totalCount, request.Page, request.PageSize);
    }

    private IQueryable<Listing> BuildBaseQuery(bool onlyMine)
    {
        var query = _db.Listings
            .Include(l => l.Channel)
            .ThenInclude(c => c.Stats)
            .Include(l => l.Channel)
            .ThenInclude(c => c.Admins)
            .Include(l => l.AdFormats)
            .AsQueryable();

        if (onlyMine)
        {
            query = query.Where(l => l.Channel.OwnerUserId == _currentUser.UserId);
        }
        else
        {
            query = query
                .Where(l => l.Status == ListingStatus.Published)
                .Where(l => l.Channel.Status == ChannelStatus.Verified);
        }

        return query;
    }

    private static IQueryable<Listing> ApplyFilters(IQueryable<Listing> query, SearchListingsRequest request)
    {
        if (request.MinPrice.HasValue)
            query = query.Where(l => l.AdFormats.Any(f => f.PriceInTon >= request.MinPrice.Value));

        if (request.MaxPrice.HasValue)
            query = query.Where(l => l.AdFormats.Any(f => f.PriceInTon <= request.MaxPrice.Value));

        if (request.MinSubscribers.HasValue)
            query = query.Where(l => l.Channel.Stats != null && l.Channel.Stats.SubscriberCount >= request.MinSubscribers.Value);

        if (request.MinAvgViews.HasValue)
            query = query.Where(l => l.Channel.Stats != null && l.Channel.Stats.AvgViewsPerPost >= request.MinAvgViews.Value);

        if (!string.IsNullOrEmpty(request.Language))
            query = query.Where(l => l.Channel.Stats != null && l.Channel.Stats.PrimaryLanguage == request.Language);

        return query;
    }

    private async Task<List<ListingDto>> ExecuteQueryAsync(IQueryable<Listing> query, SearchListingsRequest request, CancellationToken ct)
    {
        return await query
            .OrderByDescending(l => l.Channel.Stats != null ? l.Channel.Stats.SubscriberCount : 0)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new ListingDto(
                l.Id,
                l.ChannelId,
                l.Channel.Username,
                l.Channel.Title,
                l.Title,
                l.Description,
                l.Status.ToString(),
                l.AdFormats.Select(f => new SearchListingAdFormatDto(
                    f.Id,
                    f.FormatType.ToString(),
                    f.PriceInTon,
                    f.DurationHours,
                    f.Terms
                )).ToList(),
                l.Channel.Stats != null ? l.Channel.Stats.SubscriberCount : 0,
                l.Channel.Stats != null ? l.Channel.Stats.AvgViewsPerPost : 0,
                l.Channel.Stats != null ? l.Channel.Stats.AvgReachPerPost : 0,
                l.Channel.Stats != null ? l.Channel.Stats.PrimaryLanguage : null,
                l.Channel.Stats != null ? l.Channel.Stats.PremiumSubscriberCount : null,
                l.Channel.OwnerUserId == _currentUser.UserId || l.Channel.Admins.Any(a => a.UserId == _currentUser.UserId && a.CanManageListings)
            ))
            .ToListAsync(ct);
    }
}
