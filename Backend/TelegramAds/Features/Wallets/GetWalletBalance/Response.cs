namespace TelegramAds.Features.Wallets.GetWalletBalance;

public sealed record GetWalletBalanceResponse(
    decimal Balance,
    DateTime UpdatedAt
);
