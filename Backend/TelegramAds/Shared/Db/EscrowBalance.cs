namespace TelegramAds.Shared.Db;

public sealed class EscrowBalance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public decimal TotalEarnedInTon { get; set; }
    public decimal AvailableBalanceInTon { get; set; }
    public decimal LockedInDealsInTon { get; set; }
    public decimal WithdrawnInTon { get; set; }

    public string Currency { get; set; } = "TON";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
