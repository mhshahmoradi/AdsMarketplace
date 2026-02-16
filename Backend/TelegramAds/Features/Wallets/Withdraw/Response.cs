namespace TelegramAds.Features.Wallets.Withdraw;

public sealed record WithdrawResponse(
    Guid WithdrawalId,
    decimal Amount,
    decimal RemainingBalance,
    string DestinationAddress,
    DateTime CreatedAt
);
