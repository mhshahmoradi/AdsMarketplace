namespace TelegramAds.Features.Applications.ReviewApplication;

public sealed record ReviewApplicationResponse(
    Guid ApplicationId,
    string Status,
    Guid? DealId,
    DateTime UpdatedAt
);
