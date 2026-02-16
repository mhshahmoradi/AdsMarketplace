using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.UpdateListing;

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

    public async Task<UpdateListingResponse?> HandleAsync(Guid listingId, UpdateListingRequest request, CancellationToken ct)
    {
        var listing = await _db.Listings
            .Include(l => l.Channel)
            .ThenInclude(c => c.Admins)
            .Include(l => l.AdFormats)
            .FirstOrDefaultAsync(l => l.Id == listingId, ct);

        if (listing == null)
            return null;

        if (!HasPermission(listing.Channel))
            return null;


        if (request.Title != null)
            listing.Title = request.Title;

        if (request.Description != null)
            listing.Description = request.Description;

        if (request.Status != null && Enum.TryParse<ListingStatus>(request.Status, ignoreCase: true, out var status))
            listing.Status = status;

        if (request.AdFormats != null)
        {
            var existingFormats = await _db.ListingAdFormats
                .Where(f => f.ListingId == listingId)
                .ToListAsync(ct);

            _db.ListingAdFormats.RemoveRange(existingFormats);

            foreach (var format in request.AdFormats)
            {
                if (!Enum.TryParse<AdFormatType>(format.FormatType, ignoreCase: true, out var formatType))
                    continue;

                _db.ListingAdFormats.Add(new ListingAdFormat
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    FormatType = formatType,
                    PriceInTon = format.PriceInTon,
                    DurationHours = format.DurationHours,
                    Terms = format.Terms
                });
            }
        }

        listing.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new UpdateListingResponse(
            listing.Id,
            listing.ChannelId,
            listing.Title,
            listing.Description,
            listing.Status.ToString(),
            listing.AdFormats.Select(f => new AdFormatDto(
                f.Id,
                f.FormatType.ToString(),
                f.PriceInTon,
                f.DurationHours,
                f.Terms
            )).ToList(),
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
