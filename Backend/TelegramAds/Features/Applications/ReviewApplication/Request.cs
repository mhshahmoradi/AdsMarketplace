namespace TelegramAds.Features.Applications.ReviewApplication;

public sealed record ReviewApplicationRequest(
    string Decision,
    string? Reason
);
