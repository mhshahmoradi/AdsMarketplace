using Carter;
using Telegram.Bot;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Telegram;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Channels.VerifyBotAdmin;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels/{channelId:guid}/verify", async (
            Guid channelId,
            AppDbContext db,
            ITelegramBotClient botClient,
            WTelegramService wtelegram,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new VerifyBotAdminRequest(channelId, 6283680282);
            var handler = new Handler(db, botClient, wtelegram, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("VerifyBotAdmin")
        .WithTags("Channels")
        .Produces<VerifyBotAdminResponse>();
    }
}


