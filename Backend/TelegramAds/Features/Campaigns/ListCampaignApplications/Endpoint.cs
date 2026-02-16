using Carter;
using Microsoft.AspNetCore.Mvc;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.ListCampaignApplications;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/campaign-applications", async (
            [AsParameters] ListCampaignApplicationsRequest request,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("ListCampaignApplications")
        .WithTags("Campaigns")
        .Produces<ListCampaignApplicationsResponse>();
    }
}
