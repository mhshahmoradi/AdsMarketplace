using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Payments.GetPaymentStatus;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<GetPaymentStatusResponse>> HandleAsync(
        GetPaymentStatusRequest request,
        CancellationToken ct)
    {
        var payment = await _db.Payments
            .Include(p => p.Deal)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);

        if (payment is null)
            return Error.NotFound("Payment.NotFound", "Payment not found");

        if (payment.Deal.AdvertiserUserId != _currentUser.UserId &&
            payment.Deal.ChannelOwnerUserId != _currentUser.UserId)
            return Error.Forbidden("Payment.Forbidden", "You don't have access to this payment");

        return new GetPaymentStatusResponse(
            payment.Id,
            payment.DealId,
            payment.Status.ToString(),
            payment.InvoiceReference,
            payment.PaymentUrl,
            payment.AmountInTon,
            payment.Currency,
            payment.TransactionHash,
            payment.PaidByAddress,
            payment.ActualAmountInTon,
            payment.CreatedAt,
            payment.ExpiresAt,
            payment.ConfirmedAt
        );
    }
}
