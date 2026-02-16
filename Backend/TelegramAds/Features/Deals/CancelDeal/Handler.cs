using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Deals.CancelDeal;

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

    public async Task<CancelDealResponse> HandleAsync(CancelDealRequest request, CancellationToken ct)
    {
        var deal = await _db.Deals
            .Include(d => d.Channel)
            .Include(d => d.Listing).ThenInclude(l => l.Channel)
            .FirstOrDefaultAsync(d => d.Id == request.DealId, ct);

        if (deal is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Deal not found", 404);
        }

        var channelId = deal.ChannelId ?? deal.Listing?.ChannelId;
        var channelOwnerId = deal.Channel?.OwnerUserId ?? deal.Listing?.Channel?.OwnerUserId;

        var isAdvertiser = deal.AdvertiserUserId == _currentUser.UserId;
        var isChannelOwner = channelOwnerId.HasValue && channelOwnerId.Value == _currentUser.UserId;
        var isAdmin = channelId.HasValue && await _db.ChannelAdmins.AnyAsync(a =>
            a.ChannelId == channelId.Value &&
            a.UserId == _currentUser.UserId &&
            a.CanManageDeals, ct);

        if (!isAdvertiser && !isChannelOwner && !isAdmin)
        {
            throw new AppException(ErrorCodes.Forbidden, "You are not a party to this deal", 403);
        }

        if (!DealStateMachine.CanCancel(deal.Status))
        {
            throw new AppException(ErrorCodes.InvalidState, $"Cannot cancel deal in {deal.Status} status");
        }

        var previousStatus = deal.Status;
        DealStateMachine.ValidateTransition(deal.Status, DealStatus.Cancelled);
        deal.Status = DealStatus.Cancelled;
        deal.UpdatedAt = _clock.UtcNow;

        var dealEvent = new DealEvent
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            FromStatus = previousStatus,
            ToStatus = DealStatus.Cancelled,
            ActorUserId = _currentUser.UserId,
            EventType = DealEventType.Cancelled,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                CancelledBy = _currentUser.UserId,
                request.Reason
            }),
            CreatedAt = _clock.UtcNow
        };
        _db.DealEvents.Add(dealEvent);

        string? refundStatus = null;
        if (previousStatus == DealStatus.AwaitingPayment && !string.IsNullOrEmpty(deal.EscrowAddress))
        {
            refundStatus = "RefundPending";
        }

        await _db.SaveChangesAsync(ct);

        return new CancelDealResponse(deal.Id, deal.Status.ToString(), refundStatus);
    }
}
