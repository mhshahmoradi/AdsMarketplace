using Carter;
using Telegram.Bot;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Channels.AddChannel;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels", async (
            AddChannelRequest request,
            AppDbContext db,
            ITelegramBotClient botClient,
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

            var handler = new Handler(db, botClient, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Created($"/api/channels/{response.ChannelId}", response);
        })
        .WithName("AddChannel")
        .WithTags("Channels")
        .Produces<AddChannelResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}
