namespace TelegramAds.Shared.Ton;

public sealed class WithdrawalOptions
{
    public const string SectionName = "Withdrawals";

    public decimal MinimumAmountTon { get; set; } = 1.0m;
    public decimal FeeAmountTon { get; set; } = 0.5m;
    public int MaxPerDay { get; set; } = 3;
    public decimal AutoApproveUnderTon { get; set; } = 10.0m;
}
