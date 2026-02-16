using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Listings.ListListingApplications;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ListListingApplicationsResponse> HandleAsync(ListListingApplicationsRequest request, CancellationToken ct)
    {
        var query = _db.ListingApplications
            .Include(a => a.Listing).ThenInclude(l => l.Channel)
            .Include(a => a.Campaign)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Role))
        {
            if (request.Role.Equals("listing-owner", StringComparison.OrdinalIgnoreCase) ||
                request.Role.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
                request.Role.Equals("received", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Listing.Channel.OwnerUserId == _currentUser.UserId);
            }
            else if (request.Role.Equals("applicant", StringComparison.OrdinalIgnoreCase) ||
                     request.Role.Equals("advertiser", StringComparison.OrdinalIgnoreCase) ||
                     request.Role.Equals("sent", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.ApplicantUserId == _currentUser.UserId);
            }
        }
        else
        {
            query = query.Where(a =>
                a.Listing.Channel.OwnerUserId == _currentUser.UserId ||
                a.ApplicantUserId == _currentUser.UserId);
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
                a.Listing.Title.ToLower().Contains(search) ||
                a.Campaign.Title.ToLower().Contains(search) ||
                a.Campaign.Brief.ToLower().Contains(search) ||
                a.Listing.Channel.Username.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new ListingApplicationSummaryDto(
                a.Id,
                a.Status.ToString(),
                a.ProposedPriceInTon,
                a.Message,
                a.CreatedAt,
                new ApplicationListingDto(
                    a.ListingId,
                    a.Listing.Title,
                    a.Listing.Description,
                    a.Listing.Channel.Username,
                    a.Listing.Channel.Title
                ),
                new ListingAppCampaignDto(
                    a.CampaignId,
                    a.Campaign.Title,
                    a.Campaign.Brief,
                    a.Campaign.BudgetInTon
                )
            ))
            .ToListAsync(ct);

        return new ListListingApplicationsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
