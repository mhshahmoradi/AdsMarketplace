using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.ChangeListingStatus;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/listings/{id:guid}/status", async (
            Guid id,
            ChangeListingStatusRequest request,
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
        .WithName("ChangeListingStatus")
        .WithTags("Listings")
        .Produces<ChangeListingStatusResponse>()
        .Produces(404);
    }
}
