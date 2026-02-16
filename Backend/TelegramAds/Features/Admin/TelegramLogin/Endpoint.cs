using Carter;
using TelegramAds.Shared.Telegram;

namespace TelegramAds.Features.Admin.TelegramLogin;

public sealed class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/telegram-login")
            .WithTags("Admin");

        group.MapGet("/status", (WTelegramService wtelegram) =>
        {
            var response = new LoginStatusResponse(
                wtelegram.IsConnected,
                wtelegram.IsWaitingForCode,
                wtelegram.IsWaitingForPassword);
            return Results.Ok(response);
        })
        .WithName("GetTelegramLoginStatus")
        .Produces<LoginStatusResponse>();

        group.MapPost("/login", async (TelegramLoginRequest request, WTelegramService wtelegram) =>
        {
            var result = await wtelegram.StartLoginAsync(request.PhoneNumber, request.Code);
            return Results.Ok(new TelegramLoginResponse(result.Success, result.Message, result.Step.ToString()));
        })
        .WithName("TelegramLogin")
        .Produces<TelegramLoginResponse>();

        group.MapPost("/verify-password", async (SubmitPasswordRequest request, WTelegramService wtelegram) =>
        {
            var result = await wtelegram.SubmitPasswordAsync(request.Password);
            return Results.Ok(new TelegramLoginResponse(result.Success, result.Message, result.Step.ToString()));
        })
        .WithName("VerifyTelegramPassword")
        .Produces<TelegramLoginResponse>();
    }
}
