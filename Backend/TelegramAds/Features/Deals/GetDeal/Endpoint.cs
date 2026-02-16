using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Deals.GetDeal;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/deals/{dealId:guid}", async (
            Guid dealId,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new GetDealRequest(dealId);
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("GetDeal")
        .WithTags("Deals")
        .Produces<GetDealResponse>();
    }
}
