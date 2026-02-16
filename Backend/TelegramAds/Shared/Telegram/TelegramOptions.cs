namespace TelegramAds.Shared.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = null!;
    public int ApiId { get; set; }
    public string ApiHash { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? SessionPath { get; set; }
    public string? WebhookUrl { get; set; }
}
