using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.SearchCampaigns;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/campaigns", async (
            [AsParameters] SearchParams p,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new SearchCampaignsRequest(
                p.SearchTerm,
                p.MinBudget,
                p.MaxBudget,
                p.Language,
                p.OnlyMine ?? false,
                p.Page > 0 ? p.Page : 1,
                p.PageSize > 0 && p.PageSize <= 50 ? p.PageSize : 20
            );
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("SearchCampaigns")
        .WithTags("Campaigns")
        .Produces<SearchCampaignsResponse>();
    }
}

public sealed record SearchParams(
    string? SearchTerm = null,
    int? MinBudget = null,
    int? MaxBudget = null,
    string? Language = null,
    bool? OnlyMine = null,
    int Page = 1,
    int PageSize = 20
);
