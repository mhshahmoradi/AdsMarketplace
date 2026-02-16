using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Campaigns.CreateCampaign;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/campaigns", async (
            CreateCampaignRequest request,
            AppDbContext db,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var handler = new Handler(db, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Created($"/api/campaigns/{response.Id}", response);
        })
        .WithName("CreateCampaign")
        .WithTags("Campaigns")
        .Produces<CreateCampaignResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}
