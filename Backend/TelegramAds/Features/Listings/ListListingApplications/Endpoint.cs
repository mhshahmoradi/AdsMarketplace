using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Listings.ListListingApplications;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listing-applications", async (
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct,
            string? role = null,
            string? status = null,
            string? search = null,
            int page = 1,
            int pageSize = 20) =>
        {
            var request = new ListListingApplicationsRequest(role, status, search, page > 0 ? page : 1, pageSize > 0 ? pageSize : 20);
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithTags("Listings")
        .WithName("ListListingApplications")
        .Produces<ListListingApplicationsResponse>();
    }
}
