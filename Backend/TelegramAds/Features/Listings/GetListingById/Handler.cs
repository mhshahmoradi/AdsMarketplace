using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Listings.GetListingById;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetListingByIdResponse?> HandleAsync(Guid listingId, CancellationToken ct)
    {
        var listing = await _db.Listings
            .Include(l => l.Channel)
            .ThenInclude(c => c.Stats)
            .Include(l => l.Channel)
            .ThenInclude(c => c.Admins)
            .Include(l => l.AdFormats)
            .FirstOrDefaultAsync(l => l.Id == listingId, ct);

        if (listing == null)
            return null;

        var isMine = listing.Channel.OwnerUserId == _currentUser.UserId ||
                     listing.Channel.Admins.Any(a => a.UserId == _currentUser.UserId && a.CanManageListings);

        return new GetListingByIdResponse(
            listing.Id,
            listing.ChannelId,
            listing.Channel.Username,
            listing.Channel.Title,
            listing.Title,
            listing.Description,
            listing.Status.ToString(),
            listing.AdFormats.Select(f => new ListingAdFormatDto(
                f.Id,
                f.FormatType.ToString(),
                f.PriceInTon,
                f.DurationHours,
                f.Terms
            )).ToList(),
            listing.Channel.Stats?.SubscriberCount ?? 0,
            listing.Channel.Stats?.AvgViewsPerPost ?? 0,
            listing.Channel.Stats?.AvgReachPerPost ?? 0,
            listing.Channel.Stats?.PrimaryLanguage,
            listing.Channel.Stats?.LanguagesGraphJson,
            listing.Channel.Stats?.PremiumSubscriberCount,
            isMine,
            listing.CreatedAt,
            listing.UpdatedAt
        );
    }
}
