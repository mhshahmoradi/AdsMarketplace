using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.ChangeListingStatus;

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

    public async Task<ChangeListingStatusResponse?> HandleAsync(Guid listingId, ChangeListingStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ListingStatus>(request.Status, ignoreCase: true, out var status))
            throw new AppException(ErrorCodes.ValidationFailed, $"Invalid status: {request.Status}");

        var listing = await _db.Listings
            .Include(l => l.Channel)
            .ThenInclude(c => c.Admins)
            .FirstOrDefaultAsync(l => l.Id == listingId, ct);

        if (listing == null)
            return null;

        if (!HasPermission(listing.Channel))
            return null;

        listing.Status = status;
        listing.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new ChangeListingStatusResponse(
            listing.Id,
            listing.Status.ToString(),
            listing.UpdatedAt
        );
    }

    private bool HasPermission(Channel channel)
    {
        if (channel.OwnerUserId == _currentUser.UserId)
            return true;

        var admin = channel.Admins.FirstOrDefault(a => a.UserId == _currentUser.UserId);
        return admin?.CanManageListings == true;
    }
}
