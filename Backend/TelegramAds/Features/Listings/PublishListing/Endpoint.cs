using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.PublishListing;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/listings/{listingId:guid}/publish", async (
            Guid listingId,
            AppDbContext db,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new PublishListingRequest(listingId);
            var handler = new Handler(db, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("PublishListing")
        .WithTags("Listings")
        .Produces<PublishListingResponse>();
    }
}
