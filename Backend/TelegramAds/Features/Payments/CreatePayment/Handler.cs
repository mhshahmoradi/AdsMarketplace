using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TelegramAds.Features.Deals;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;
using TelegramAds.Shared.Ton;

namespace TelegramAds.Features.Payments.CreatePayment;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;
    private readonly TonPaymentService _tonPaymentService;
    private readonly TonOptions _tonOptions;
    private readonly IClock _clock;

    public Handler(
        AppDbContext db,
        CurrentUser currentUser,
        TonPaymentService tonPaymentService,
        TonOptions tonOptions,
        IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _tonPaymentService = tonPaymentService;
        _tonOptions = tonOptions;
        _clock = clock;
    }

    public async Task<ErrorOr<CreatePaymentResponse>> HandleAsync(
        CreatePaymentRequest request,
        CancellationToken ct)
    {
        var deal = await _db.Deals
            .Include(d => d.Campaign)
            .FirstOrDefaultAsync(d => d.Id == request.DealId, ct);

        if (deal is null)
            return Error.NotFound("Deal.NotFound", "Deal not found");

        if (deal.AdvertiserUserId != _currentUser.UserId)
            return Error.Forbidden("Payment.Forbidden", "Only the advertiser can create payment for this deal");

        if (deal.Status != DealStatus.Agreed && deal.Status != DealStatus.AwaitingPayment)
            return Error.Validation("Payment.InvalidStatus", "Deal must be in Agreed or AwaitingPayment status to create payment");

        var confirmedPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.DealId == request.DealId && p.Status == PaymentStatus.Confirmed, ct);

        if (confirmedPayment is not null)
            return Error.Conflict("Payment.AlreadyPaid", "This deal has already been paid");

        var existingPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.DealId == request.DealId && p.Status == PaymentStatus.Pending, ct);

        if (existingPayment is not null)
        {
            return new CreatePaymentResponse(
                existingPayment.Id,
                existingPayment.InvoiceReference,
                existingPayment.PaymentUrl,
                existingPayment.AmountInTon,
                existingPayment.Currency,
                existingPayment.ExpiresAt
            );
        }

        var paymentId = Guid.NewGuid();
        var paymentUrl = _tonPaymentService.GeneratePaymentUrl(
            deal.AgreedPriceInTon,
            paymentId);

        var now = _clock.UtcNow;
        var expiresAt = now.AddHours(_tonOptions.InvoiceExpirationHours);

        var payment = new Payment
        {
            Id = paymentId,
            DealId = deal.Id,
            InvoiceReference = paymentId.ToString(),
            InvoiceText = $"Payment for deal {deal.Id}",
            PaymentUrl = paymentUrl,
            AmountInTon = deal.AgreedPriceInTon,
            Currency = request.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            Description = $"Payment for deal {deal.Id} - {deal.Campaign.Title}"
        };

        _db.Payments.Add(payment);

        if (deal.Status == DealStatus.Agreed)
        {
            deal.Status = DealStatus.AwaitingPayment;
            deal.UpdatedAt = now;

            var dealEvent = new DealEvent
            {
                Id = Guid.NewGuid(),
                DealId = deal.Id,
                FromStatus = DealStatus.Agreed,
                ToStatus = DealStatus.AwaitingPayment,
                EventType = DealEventType.PaymentRequested,
                ActorUserId = _currentUser.UserId,
                CreatedAt = now
            };

            _db.DealEvents.Add(dealEvent);
        }

        await _db.SaveChangesAsync(ct);

        return new CreatePaymentResponse(
            payment.Id,
            payment.InvoiceReference,
            payment.PaymentUrl,
            payment.AmountInTon,
            payment.Currency,
            payment.ExpiresAt
        );
    }
}
