using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Deals.SubmitCreative;

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

    public async Task<SubmitCreativeResponse> HandleAsync(Guid dealId, SubmitCreativeRequest request, CancellationToken ct)
    {
        var deal = await _db.Deals
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Deal not found", 404);
        }

        if (deal.AdvertiserUserId != _currentUser.UserId)
        {
            throw new AppException(ErrorCodes.Forbidden, "Only the advertiser can submit creative", 403);
        }

        if (deal.Status != DealStatus.Paid && deal.Status != DealStatus.CreativeDraft)
        {
            throw new AppException(ErrorCodes.InvalidState,
                $"Cannot submit creative when deal is in {deal.Status} status. Deal must be in Paid or CreativeDraft status.", 400);
        }

        var now = _clock.UtcNow;
        var previousStatus = deal.Status;

        deal.CreativeText = request.CreativeText;
        deal.Status = DealStatus.CreativeReview;
        deal.UpdatedAt = now;

        var dealEvent = new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            FromStatus = previousStatus,
            ToStatus = DealStatus.CreativeReview,
            EventType = DealEventType.CreativeSubmitted,
            ActorUserId = _currentUser.UserId,
            CreatedAt = now
        };

        _db.DealEvents.Add(dealEvent);

        await _db.SaveChangesAsync(ct);

        return new SubmitCreativeResponse(
            deal.Id,
            deal.Status.ToString(),
            deal.CreativeText,
            now
        );
    }
}
