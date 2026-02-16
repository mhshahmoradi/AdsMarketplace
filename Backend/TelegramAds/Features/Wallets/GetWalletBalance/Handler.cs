using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Wallets.GetWalletBalance;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetWalletBalanceResponse?> HandleAsync(CancellationToken ct)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.UserId == _currentUser.UserId, ct);

        if (wallet is null)
            return null;

        return new GetWalletBalanceResponse(
            wallet.Balance,
            wallet.UpdatedAt
        );
    }
}
