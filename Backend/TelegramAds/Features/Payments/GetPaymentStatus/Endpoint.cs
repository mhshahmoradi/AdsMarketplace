using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Payments.GetPaymentStatus;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/payments/{paymentId:guid}", async (
            Guid paymentId,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new GetPaymentStatusRequest(paymentId);
            var handler = new Handler(db, currentUser);
            var result = await handler.HandleAsync(request, ct);

            return result.Match(
                success => Results.Ok(success),
                errors => Results.Problem(statusCode: errors[0].Type switch
                {
                    ErrorOr.ErrorType.NotFound => 404,
                    ErrorOr.ErrorType.Forbidden => 403,
                    _ => 500
                }, title: errors[0].Code, detail: errors[0].Description)
            );
        })
        .WithName("GetPaymentStatus")
        .WithTags("Payments")
        .Produces<GetPaymentStatusResponse>()
        .Produces(404)
        .Produces(403);
    }
}
