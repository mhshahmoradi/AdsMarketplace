using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Deals.SubmitCreative;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/deals/{dealId:guid}/creative/submit", async (
            [FromRoute] Guid dealId,
            [FromBody] SubmitCreativeRequest request,
            AppDbContext db,
            CurrentUser currentUser,
            IClock clock,
            IValidator<SubmitCreativeRequest> validator,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var handler = new Handler(db, currentUser, clock);
            var response = await handler.HandleAsync(dealId, request, ct);
            return Results.Ok(response);
        })
        .WithName("SubmitCreative")
        .WithTags("Deals")
        .Produces<SubmitCreativeResponse>();
    }
}
