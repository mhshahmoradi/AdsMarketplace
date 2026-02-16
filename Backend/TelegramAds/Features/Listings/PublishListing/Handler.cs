using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.PublishListing;

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

    public async Task<PublishListingResponse> HandleAsync(PublishListingRequest request, CancellationToken ct)
    {
        var listing = await _db.Listings
            .Include(l => l.Channel)
            .Include(l => l.AdFormats)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, ct);

        if (listing is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Listing not found", 404);
        }

        if (listing.Channel.Status != ChannelStatus.Verified)
        {
            throw new AppException(ErrorCodes.ChannelNotVerified, "Channel must be verified to publish listings");
        }

        var hasPermission = listing.Channel.OwnerUserId == _currentUser.UserId ||
                            await _db.ChannelAdmins.AnyAsync(a =>
                                a.ChannelId == listing.ChannelId &&
                                a.UserId == _currentUser.UserId &&
                                a.CanManageListings, ct);

        if (!hasPermission)
        {
            throw new AppException(ErrorCodes.Forbidden, "You don't have permission to publish this listing", 403);
        }

        if (listing.Status != ListingStatus.Draft && listing.Status != ListingStatus.Paused)
        {
            throw new AppException(ErrorCodes.InvalidState, $"Cannot publish listing in {listing.Status} status");
        }

        if (!listing.AdFormats.Any())
        {
            throw new AppException(ErrorCodes.ValidationFailed, "Listing must have at least one ad format");
        }

        listing.Status = ListingStatus.Published;
        listing.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new PublishListingResponse(listing.Id, listing.Title, listing.Status.ToString());
    }
}
