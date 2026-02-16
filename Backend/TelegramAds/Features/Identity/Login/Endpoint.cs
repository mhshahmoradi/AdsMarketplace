using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Identity.Login;

public sealed class Endpoint : ICarterModule
{
    private const string InitDataHeader = "Authorization";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/identity/login", async (
            HttpContext httpContext,
            AppDbContext db,
            TelegramInitDataValidator validator,
            JwtService jwtService,
            IClock clock,
            CancellationToken ct) =>
        {
            var initData = httpContext.Request.Headers[InitDataHeader].FirstOrDefault();
            if (string.IsNullOrEmpty(initData))
            {
                throw new AppException(ErrorCodes.Unauthorized, $"Missing {InitDataHeader} header", 401);
            }

            var handler = new Handler(db, validator, jwtService, clock);
            var response = await handler.HandleAsync(initData, ct);
            return Results.Ok(response);
        })
        .WithName("Login")
        .WithTags("Identity")
        .Produces<LoginResponse>();
    }
}
