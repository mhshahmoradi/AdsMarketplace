using Carter;
using Telegram.Bot;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Channels.SyncAdmins;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels/{channelId:guid}/sync-admins", async (
            Guid channelId,
            AppDbContext db,
            ITelegramBotClient bot,
            IClock clock,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new SyncAdminsRequest(channelId);
            var handler = new Handler(db, bot, clock, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("SyncChannelAdmins")
        .WithTags("Channels")
        .Produces<SyncAdminsResponse>();
    }
}
