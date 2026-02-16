namespace TelegramAds.Features.Admin.TelegramLogin;

public sealed record TelegramLoginRequest(string PhoneNumber, string? Code = null);
public sealed record SubmitPasswordRequest(string Password);
public sealed record TelegramLoginResponse(bool Success, string Message, string Step);
public sealed record LoginStatusResponse(bool IsConnected, bool IsWaitingForCode, bool IsWaitingForPassword);
