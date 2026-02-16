using Microsoft.AspNetCore.Identity;

namespace TelegramAds.Shared.Db;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public long TgUserId { get; set; }
    public string? TgUsername { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
