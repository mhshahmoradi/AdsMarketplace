namespace TelegramAds.Features.Wallets.Withdraw;

public sealed record WithdrawRequest(
    string DestinationAddress,
    decimal Amount
);
