using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;

namespace TelegramAds.Features.Deals;

public static class DealStateMachine
{
    private static readonly Dictionary<DealStatus, DealStatus[]> ValidTransitions = new()
    {
        [DealStatus.Agreed] = [DealStatus.AwaitingPayment, DealStatus.Cancelled, DealStatus.Expired],
        [DealStatus.AwaitingPayment] = [DealStatus.Paid, DealStatus.Cancelled, DealStatus.Expired],
        [DealStatus.Paid] = [DealStatus.CreativeDraft, DealStatus.Refunded],
        [DealStatus.CreativeDraft] = [DealStatus.CreativeReview, DealStatus.Cancelled, DealStatus.Refunded],
        [DealStatus.CreativeReview] = [DealStatus.CreativeDraft, DealStatus.Scheduled, DealStatus.Cancelled, DealStatus.Refunded],
        [DealStatus.Scheduled] = [DealStatus.Posted, DealStatus.Cancelled, DealStatus.Refunded],
        [DealStatus.Posted] = [DealStatus.Verified, DealStatus.Refunded],
        [DealStatus.Verified] = [DealStatus.Released, DealStatus.Refunded],
        [DealStatus.Released] = [],
        [DealStatus.Refunded] = [],
        [DealStatus.Cancelled] = [],
        [DealStatus.Expired] = []
    };

    public static bool CanTransition(DealStatus from, DealStatus to)
    {
        return ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static void ValidateTransition(DealStatus from, DealStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new AppException(ErrorCodes.DealStateInvalid, $"Cannot transition deal from {from} to {to}");
        }
    }

    public static bool CanCancel(DealStatus status)
    {
        return status is DealStatus.Agreed
            or DealStatus.AwaitingPayment
            or DealStatus.CreativeDraft
            or DealStatus.CreativeReview
            or DealStatus.Scheduled;
    }

    public static bool IsFinalState(DealStatus status)
    {
        return status is DealStatus.Released or DealStatus.Refunded or DealStatus.Cancelled or DealStatus.Expired;
    }
}
