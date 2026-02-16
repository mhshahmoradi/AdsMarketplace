using Carter;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Channels.GetChannel;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/channels/{channelId:guid}", async (
            Guid channelId,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var request = new GetChannelRequest(channelId);
            var handler = new Handler(db);
            var response = await handler.HandleAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("GetChannel")
        .WithTags("Channels")
        .Produces<GetChannelResponse>();
    }
}
