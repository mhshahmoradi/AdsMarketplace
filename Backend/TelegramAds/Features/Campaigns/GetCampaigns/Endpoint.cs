using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.GetCampaigns;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/campaigns/all", async (
            bool? onlyMine,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new GetCampaignsRequest(onlyMine ?? false);
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("GetCampaigns")
        .WithTags("Campaigns")
        .Produces<GetCampaignsResponse>();
    }
}
