namespace TelegramAds.Shared.Db;

public enum PaymentStatus
{
    Pending,
    Confirmed,
    Expired,
    Cancelled,
    Refunded
}

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }

    public string InvoiceReference { get; set; } = null!;
    public string InvoiceText { get; set; } = null!;
    public string PaymentUrl { get; set; } = null!;

    public decimal AmountInTon { get; set; }
    public string Currency { get; set; } = "TON";

    public PaymentStatus Status { get; set; }

    public string? TransactionHash { get; set; }
    public long? TransactionTimestamp { get; set; }
    public string? PaidByAddress { get; set; }
    public decimal? ActualAmountInTon { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public string? Description { get; set; }

    public Deal Deal { get; set; } = null!;
    public ICollection<DealEscrowTransaction> Transactions { get; set; } = [];
}
