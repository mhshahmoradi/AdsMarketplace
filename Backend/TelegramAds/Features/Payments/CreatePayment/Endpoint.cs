using Carter;
using FluentValidation;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;
using TelegramAds.Shared.Ton;

namespace TelegramAds.Features.Payments.CreatePayment;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/create", async (
            CreatePaymentRequest request,
            AppDbContext db,
            CurrentUser currentUser,
            TonPaymentService tonPaymentService,
            TonOptions tonOptions,
            IClock clock,
            IValidator<CreatePaymentRequest> validator,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var handler = new Handler(db, currentUser, tonPaymentService, tonOptions, clock);
            var result = await handler.HandleAsync(request, ct);

            return result.Match(
                success => Results.Ok(success),
                errors => Results.Problem(statusCode: errors[0].Type switch
                {
                    ErrorOr.ErrorType.NotFound => 404,
                    ErrorOr.ErrorType.Forbidden => 403,
                    ErrorOr.ErrorType.Conflict => 409,
                    ErrorOr.ErrorType.Validation => 400,
                    _ => 500
                }, title: errors[0].Code, detail: errors[0].Description)
            );
        })
        .WithName("CreatePayment")
        .WithTags("Payments")
        .Produces<CreatePaymentResponse>()
        .ProducesValidationProblem()
        .Produces(404)
        .Produces(403)
        .Produces(409);
    }
}
