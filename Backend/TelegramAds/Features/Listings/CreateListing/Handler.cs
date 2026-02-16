using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.CreateListing;

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

    public async Task<CreateListingResponse> HandleAsync(CreateListingRequest request, CancellationToken ct)
    {
        var channel = await _db.Channels.FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);
        if (channel is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Channel not found", 404);
        }

        if (channel.Status != ChannelStatus.Verified)
        {
            throw new AppException(ErrorCodes.ChannelNotVerified, "Channel must be verified before creating listings");
        }

        var hasPermission = channel.OwnerUserId == _currentUser.UserId ||
                            await _db.ChannelAdmins.AnyAsync(a =>
                                a.ChannelId == channel.Id &&
                                a.UserId == _currentUser.UserId &&
                                a.CanManageListings, ct);

        if (!hasPermission)
        {
            throw new AppException(ErrorCodes.Forbidden, "You don't have permission to create listings for this channel", 403);
        }

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id,
            Title = request.Title,
            Description = request.Description,
            Status = ListingStatus.Draft,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };

        foreach (var format in request.AdFormats)
        {
            if (!Enum.TryParse<AdFormatType>(format.FormatType, ignoreCase: true, out var formatType))
                continue;

            listing.AdFormats.Add(new ListingAdFormat
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                FormatType = formatType,
                PriceInTon = format.PriceInTon,
                DurationHours = format.DurationHours,
                Terms = format.Terms
            });
        }

        _db.Listings.Add(listing);
        await _db.SaveChangesAsync(ct);

        return new CreateListingResponse(listing.Id, listing.ChannelId, listing.Title, listing.Status.ToString());
    }
}
