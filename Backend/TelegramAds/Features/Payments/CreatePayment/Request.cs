namespace TelegramAds.Features.Payments.CreatePayment;

public sealed record CreatePaymentRequest(
    Guid DealId,
    string Currency = "TON"
);
