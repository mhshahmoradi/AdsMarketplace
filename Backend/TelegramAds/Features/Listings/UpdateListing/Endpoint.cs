using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.UpdateListing;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/listings/{id:guid}", async (
            Guid id,
            UpdateListingRequest request,
            AppDbContext db,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, clock, currentUser);
            var response = await handler.HandleAsync(id, request, ct);

            if (response == null)
                return Results.NotFound();

            return Results.Ok(response);
        })
        .WithName("UpdateListing")
        .WithTags("Listings")
        .Produces<UpdateListingResponse>()
        .Produces(404);
    }
}
