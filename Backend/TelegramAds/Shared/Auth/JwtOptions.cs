namespace TelegramAds.Shared.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "TelegramAds";
    public string Audience { get; set; } = "TelegramAds";
    public int ExpirationMinutes { get; set; } = 1440;
}
