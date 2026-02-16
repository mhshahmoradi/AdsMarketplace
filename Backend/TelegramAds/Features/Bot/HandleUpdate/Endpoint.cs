using Carter;
using Telegram.Bot.Types;
using TelegramAds.Features.Bot.Actions;

namespace TelegramAds.Features.Bot.HandleUpdate;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/bot", async (
            Update update,
            IServiceProvider sp,
            ILogger<Endpoint> logger,
            CancellationToken ct) =>
        {
            try
            {
                var handler = new Handler(sp, logger);
                await handler.HandleAsync(update, ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Telegram update {UpdateId}", update.Id);
                return Results.Ok();
            }
        })
        .WithName("BotWebhook")
        .WithTags("Bot");
    }
}
