using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Listings.ApplyToListing;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/listings/{listingId:guid}/apply", async (
            Guid listingId,
            [FromBody] ApplyToListingRequestBody body,
            IValidator<ApplyToListingRequest> validator,
            AppDbContext db,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new ApplyToListingRequest(listingId, body.CampaignId, body.ProposedPriceInTon, body.Message);

            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                throw new AppException(ErrorCodes.ValidationFailed, validation.Errors[0].ErrorMessage);
            }

            var handler = new Handler(db, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Created($"/api/applications/{response.ApplicationId}", response);
        })
        .RequireAuthorization()
        .WithTags("Listings")
        .WithName("ApplyToListing")
        .Produces<ApplyToListingResponse>(201);
    }
}

public sealed record ApplyToListingRequestBody(
    Guid CampaignId,
    decimal ProposedPriceInTon,
    string? Message
);
