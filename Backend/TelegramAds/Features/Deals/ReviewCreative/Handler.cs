using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Deals.ReviewCreative;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;
    private readonly IClock _clock;

    public Handler(AppDbContext db, CurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ReviewCreativeResponse> HandleAsync(Guid dealId, ReviewCreativeRequest request, CancellationToken ct)
    {
        var deal = await _db.Deals
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Deal not found", 404);
        }

        if (deal.ChannelOwnerUserId != _currentUser.UserId)
        {
            throw new AppException(ErrorCodes.Forbidden, "Only the channel owner can review creative", 403);
        }

        if (deal.Status != DealStatus.CreativeReview)
        {
            throw new AppException(ErrorCodes.InvalidState,
                $"Cannot review creative when deal is in {deal.Status} status. Deal must be in CreativeReview status.", 400);
        }

        if (string.IsNullOrEmpty(deal.CreativeText))
        {
            throw new AppException(ErrorCodes.InvalidState, "No creative has been submitted yet", 400);
        }

        var now = _clock.UtcNow;
        var isAccepted = request.Decision.Equals("Accepted", StringComparison.OrdinalIgnoreCase);

        var newStatus = isAccepted ? DealStatus.Scheduled : DealStatus.CreativeDraft;
        var eventType = isAccepted ? DealEventType.CreativeApproved : DealEventType.CreativeRejected;

        deal.Status = newStatus;
        deal.UpdatedAt = now;
        deal.CreativeRejectionReason = isAccepted ? null : request.RejectionReason;

        var dealEvent = new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            FromStatus = DealStatus.CreativeReview,
            ToStatus = newStatus,
            EventType = eventType,
            ActorUserId = _currentUser.UserId,
            CreatedAt = now
        };

        _db.DealEvents.Add(dealEvent);

        await _db.SaveChangesAsync(ct);

        return new ReviewCreativeResponse(
            deal.Id,
            deal.Status.ToString(),
            request.Decision,
            request.RejectionReason,
            now
        );
    }
}
