using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Ton;

namespace TelegramAds.Features.Wallets.Withdraw;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallet/withdraw", async (
            WithdrawRequest request,
            AppDbContext db,
            CurrentUser currentUser,
            TonWalletService tonWalletService,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser, tonWalletService);
            var result = await handler.HandleAsync(request, ct);

            if (result.IsError)
                return Results.BadRequest(new { error = result.FirstError.Description });

            return Results.Ok(result.Value);
        })
        .WithName("Withdraw")
        .WithTags("Wallet")
        .Produces<WithdrawResponse>()
        .Produces(400);
    }
}
