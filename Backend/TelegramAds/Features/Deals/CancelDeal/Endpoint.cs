using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Deals.CancelDeal;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/deals/{dealId:guid}/cancel", async (
            Guid dealId,
            CancelDealRequestBody? body,
            AppDbContext db,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new CancelDealRequest(dealId, body?.Reason);
            var handler = new Handler(db, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("CancelDeal")
        .WithTags("Deals")
        .Produces<CancelDealResponse>();
    }
}

public sealed record CancelDealRequestBody(string? Reason);
