using Carter;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Channels.GetChannels;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/channels", async (
            bool? onlyMine,
            AppDbContext db,
            CurrentUser currentUser,
            CancellationToken ct) =>
        {
            var request = new GetChannelsRequest(onlyMine ?? false);
            var handler = new Handler(db, currentUser);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("GetChannels")
        .WithTags("Channels")
        .Produces<GetChannelsResponse>();
    }
}
