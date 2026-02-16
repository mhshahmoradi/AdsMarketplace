using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.ApplyToListing;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, IClock clock, CurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<ApplyToListingResponse> HandleAsync(ApplyToListingRequest request, CancellationToken ct)
    {
        var listing = await _db.Listings
            .Include(l => l.Channel)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, ct);

        if (listing is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Listing not found", 404);
        }

        if (listing.Status != ListingStatus.Published)
        {
            throw new AppException(ErrorCodes.InvalidState, "Listing is not available for applications");
        }

        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == request.CampaignId, ct);
        if (campaign is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Campaign not found", 404);
        }

        if (campaign.AdvertiserUserId != _currentUser.UserId)
        {
            throw new AppException(ErrorCodes.Forbidden, "You can only apply with your own campaigns", 403);
        }

        if (campaign.Status != CampaignStatus.Open)
        {
            throw new AppException(ErrorCodes.InvalidState, "Campaign is not open for applications");
        }

        if (listing.Channel.OwnerUserId == _currentUser.UserId)
        {
            throw new AppException(ErrorCodes.InvalidState, "You cannot apply to your own listing");
        }

        var existingApplication = await _db.ListingApplications
            .FirstOrDefaultAsync(a => a.ListingId == request.ListingId && a.CampaignId == request.CampaignId, ct);

        if (existingApplication is not null)
        {
            throw new AppException(ErrorCodes.AlreadyExists, "Application already exists for this listing and campaign");
        }

        var application = new ListingApplication
        {
            Id = Guid.NewGuid(),
            ListingId = request.ListingId,
            CampaignId = request.CampaignId,
            ApplicantUserId = _currentUser.UserId,
            ProposedPriceInTon = request.ProposedPriceInTon,
            Message = request.Message,
            Status = ApplicationStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        _db.ListingApplications.Add(application);
        await _db.SaveChangesAsync(ct);

        return new ApplyToListingResponse(
            application.Id,
            application.ListingId,
            application.CampaignId,
            application.Status.ToString(),
            application.ProposedPriceInTon,
            application.CreatedAt
        );
    }
}
