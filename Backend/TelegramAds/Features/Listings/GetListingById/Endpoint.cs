using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Listings.GetListingById;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listings/{id:guid}", async (
            Guid id,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(id, ct);

            if (response == null)
                return Results.NotFound();

            return Results.Ok(response);
        })
        .WithName("GetListingById")
        .WithTags("Listings")
        .Produces<GetListingByIdResponse>()
        .Produces(404);
    }
}
