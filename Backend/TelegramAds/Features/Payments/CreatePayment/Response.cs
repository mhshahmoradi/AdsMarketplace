namespace TelegramAds.Features.Payments.CreatePayment;

public sealed record CreatePaymentResponse(
    Guid PaymentId,
    string InvoiceReference,
    string PaymentUrl,
    decimal AmountInTon,
    string Currency,
    DateTime ExpiresAt
);
