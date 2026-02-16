using Carter;
using FluentValidation;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Campaigns.ApplyToCampaign;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/campaigns/{campaignId:guid}/apply", async (
            Guid campaignId,
            ApplyToCampaignRequestBody body,
            IValidator<ApplyToCampaignRequest> validator,
            AppDbContext db,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new ApplyToCampaignRequest(campaignId, body.ChannelId, body.ProposedPriceInTon, body.Note);
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                throw new AppException(ErrorCodes.ValidationFailed, validation.Errors[0].ErrorMessage);
            }

            var handler = new Handler(db, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Created($"/api/campaigns/{campaignId}/applications/{response.ApplicationId}", response);
        })
        .WithName("ApplyToCampaign")
        .WithTags("Campaigns")
        .Produces<ApplyToCampaignResponse>(201);
    }
}

public sealed record ApplyToCampaignRequestBody(
    Guid ChannelId,
    decimal ProposedPriceInTon,
    string? Note
);
