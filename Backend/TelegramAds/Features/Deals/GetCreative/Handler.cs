using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;

namespace TelegramAds.Features.Deals.GetCreative;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetCreativeResponse> HandleAsync(Guid dealId, CancellationToken ct)
    {
        var deal = await _db.Deals
            .Include(d => d.Events.Where(e =>
                e.EventType == DealEventType.CreativeSubmitted ||
                e.EventType == DealEventType.CreativeApproved ||
                e.EventType == DealEventType.CreativeRejected))
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Deal not found", 404);
        }

        var isParticipant = deal.AdvertiserUserId == _currentUser.UserId ||
                            deal.ChannelOwnerUserId == _currentUser.UserId;

        if (!isParticipant)
        {
            throw new AppException(ErrorCodes.Forbidden, "You are not a participant in this deal", 403);
        }

        var submittedEvent = deal.Events
            .Where(e => e.EventType == DealEventType.CreativeSubmitted)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        var reviewedEvent = deal.Events
            .Where(e => e.EventType == DealEventType.CreativeApproved ||
                       e.EventType == DealEventType.CreativeRejected)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        return new GetCreativeResponse(
            deal.Id,
            deal.Status.ToString(),
            deal.CreativeText,
            submittedEvent?.CreatedAt,
            reviewedEvent?.CreatedAt,
            deal.CreativeRejectionReason
        );
    }
}
