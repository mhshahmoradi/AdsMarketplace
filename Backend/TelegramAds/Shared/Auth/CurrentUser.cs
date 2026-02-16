using System.Security.Claims;

namespace TelegramAds.Shared.Auth;

public sealed class CurrentUser
{
    public Guid UserId { get; private set; }
    public long TgUserId { get; private set; }
    public string? Username { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public void SetFromClaimsPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            IsAuthenticated = false;
            return;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tgUserIdClaim = principal.FindFirst("tg_user_id")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId) && long.TryParse(tgUserIdClaim, out var tgUserId))
        {
            UserId = userId;
            TgUserId = tgUserId;
            Username = principal.FindFirst(ClaimTypes.Name)?.Value;
            IsAuthenticated = true;
        }
        else
        {
            IsAuthenticated = false;
        }
    }
}
