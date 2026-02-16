using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Ton;

namespace TelegramAds.Features.Wallets.Withdraw;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;
    private readonly TonWalletService _tonWalletService;

    public Handler(AppDbContext db, CurrentUser currentUser, TonWalletService tonWalletService)
    {
        _db = db;
        _currentUser = currentUser;
        _tonWalletService = tonWalletService;
    }

    public async Task<ErrorOr<WithdrawResponse>> HandleAsync(WithdrawRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
            return Error.Validation("Amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(request.DestinationAddress))
            return Error.Validation("Destination address is required");

        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.UserId == _currentUser.UserId, ct);

        if (wallet is null)
            return Error.NotFound("Wallet not found");

        if (wallet.Balance < request.Amount)
            return Error.Validation($"Insufficient balance. Available: {wallet.Balance} TON");

        var transferResult = await _tonWalletService.TransferAsync(
            request.DestinationAddress,
            request.Amount,
            "Withdrawal from system");

        if (transferResult.IsError)
            return transferResult.Errors;

        wallet.Balance -= request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var withdrawal = new Withdrawal
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            AmountInTon = request.Amount,
            FeeInTon = 0,
            NetAmountInTon = request.Amount,
            DestinationAddress = request.DestinationAddress,
            Status = WithdrawalStatus.Completed,
            TransactionHash = transferResult.Value,
            BlockchainConfirmedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _db.Withdrawals.Add(withdrawal);
        await _db.SaveChangesAsync(ct);

        return new WithdrawResponse(
            withdrawal.Id,
            withdrawal.AmountInTon,
            wallet.Balance,
            withdrawal.DestinationAddress,
            withdrawal.CreatedAt
        );
    }
}
