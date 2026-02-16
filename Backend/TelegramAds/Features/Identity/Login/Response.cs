namespace TelegramAds.Features.Identity.Login;

public sealed record LoginResponse(
    string Token,
    Guid UserId,
    long TgUserId,
    string? Username,
    string? FirstName,
    string? LastName,
    bool IsNewUser
);
