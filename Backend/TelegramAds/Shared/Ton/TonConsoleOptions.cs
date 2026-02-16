namespace TelegramAds.Shared.Ton;

public sealed class TonConsoleOptions
{
    public const string SectionName = "TonConsole";

    public string ApiKey { get; set; } = null!;
    public string BaseUrl { get; set; } = "https://tonconsole.com/api/v1";
    public string MerchantWalletAddress { get; set; } = null!;
    public int DefaultInvoiceLifetimeSeconds { get; set; } = 1800;
}
