using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.GetCampaignById;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/campaigns/{id:guid}", async (
            Guid id,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(id, ct);

            if (response == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(response);
        })
        .WithName("GetCampaignById")
        .WithTags("Campaigns")
        .Produces<GetCampaignByIdResponse>()
        .Produces(404);
    }
}
