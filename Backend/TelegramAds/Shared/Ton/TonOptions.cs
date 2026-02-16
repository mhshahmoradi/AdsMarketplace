namespace TelegramAds.Shared.Ton;

public sealed class TonOptions
{
    public const string SectionName = "Ton";

    public string Network { get; set; } = "testnet";
    public string TonApiKey { get; set; } = "";
    public string DepositWalletAddress { get; set; } = null!;
    public int InvoiceExpirationHours { get; set; } = 24;
}
