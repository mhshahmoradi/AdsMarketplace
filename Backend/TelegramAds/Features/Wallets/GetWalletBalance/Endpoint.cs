using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Wallets.GetWalletBalance;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallet/balance", async (
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(ct);

            if (response is null)
                return Results.NotFound(new { error = "Wallet not found" });

            return Results.Ok(response);
        })
        .WithName("GetWalletBalance")
        .WithTags("Wallets")
        .Produces<GetWalletBalanceResponse>()
        .Produces(404);
    }
}
