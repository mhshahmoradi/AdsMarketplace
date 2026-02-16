using Carter;
using Microsoft.AspNetCore.Mvc;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Deals.ListDeals;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/deals", async (
            [AsParameters] ListDealsRequest request,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("ListDeals")
        .WithTags("Deals")
        .Produces<ListDealsResponse>();
    }
}
