namespace TelegramAds.Features.Payments.GetPaymentStatus;

public sealed record GetPaymentStatusResponse(
    Guid PaymentId,
    Guid DealId,
    string Status,
    string InvoiceReference,
    string PaymentUrl,
    decimal AmountInTon,
    string Currency,
    string? TransactionHash,
    string? PaidByAddress,
    decimal? ActualAmountInTon,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ConfirmedAt
);
