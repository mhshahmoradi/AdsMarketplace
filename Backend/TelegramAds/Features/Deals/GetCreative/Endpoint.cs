using Carter;
using Microsoft.AspNetCore.Mvc;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Auth;

namespace TelegramAds.Features.Deals.GetCreative;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/deals/{dealId:guid}/creative", async (
            [FromRoute] Guid dealId,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(dealId, ct);
            return Results.Ok(response);
        })
        .WithName("GetCreative")
        .WithTags("Deals")
        .Produces<GetCreativeResponse>();
    }
}
