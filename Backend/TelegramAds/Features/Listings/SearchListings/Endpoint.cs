using Carter;
using Microsoft.AspNetCore.Mvc;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Listings.SearchListings;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listings", async (
            [AsParameters] SearchListingsRequest request,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("SearchListings")
        .WithTags("Listings")
        .Produces<SearchListingsResponse>();
    }
}
