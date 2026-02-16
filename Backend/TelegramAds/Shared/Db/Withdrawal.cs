namespace TelegramAds.Shared.Db;

public enum WithdrawalStatus
{
    Pending,
    Approved,
    Processing,
    Completed,
    Rejected,
    Failed
}

public sealed class Withdrawal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public decimal AmountInTon { get; set; }
    public decimal FeeInTon { get; set; }
    public decimal NetAmountInTon { get; set; }
    public string Currency { get; set; } = "TON";

    public string DestinationAddress { get; set; } = null!;

    public WithdrawalStatus Status { get; set; }

    public string? TransactionHash { get; set; }
    public DateTime? BlockchainConfirmedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? ReviewedByUser { get; set; }
    public ICollection<DealEscrowTransaction> Transactions { get; set; } = [];
}
