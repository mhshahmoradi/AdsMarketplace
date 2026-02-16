using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Applications.ReviewApplication;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/applications/{id:guid}/review", async (
            Guid id,
            [FromBody] ReviewApplicationRequest request,
            IValidator<ReviewApplicationRequest> validator,
            AppDbContext db,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                throw new AppException(ErrorCodes.ValidationFailed, validation.Errors[0].ErrorMessage);
            }

            var handler = new Handler(db, clock, currentUser);
            var response = await handler.HandleAsync(id, request, ct);
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithTags("Applications")
        .WithName("ReviewApplication")
        .Produces<ReviewApplicationResponse>();
    }
}
