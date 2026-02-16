namespace TelegramAds.Shared.Db;

public enum EscrowTransactionType
{
    PaymentReceived,
    FundsReleased,
    FundsRefunded
}

public sealed class DealEscrowTransaction
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }

    public EscrowTransactionType TransactionType { get; set; }

    public decimal AmountInTon { get; set; }

    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }

    public Guid? PaymentId { get; set; }
    public Guid? WithdrawalId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Deal Deal { get; set; } = null!;
    public ApplicationUser? FromUser { get; set; }
    public ApplicationUser? ToUser { get; set; }
    public Payment? Payment { get; set; }
    public Withdrawal? Withdrawal { get; set; }
}
