using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramAds.Features.Bot.Actions;
using TelegramAds.Features.Bot.Chat;

namespace TelegramAds.Features.Bot.HandleUpdate;

public sealed class Handler
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;

    public Handler(IServiceProvider sp, ILogger logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                await HandleMessageAsync(update.Message!, ct);
                break;
            case UpdateType.CallbackQuery:
                await HandleCallbackQueryAsync(update.CallbackQuery!, ct);
                break;
            default:
                _logger.LogDebug("Unhandled update type: {Type}", update.Type);
                break;
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        var botClient = _sp.GetRequiredService<ITelegramBotClient>();
        var chatHandler = _sp.GetRequiredService<ChatHandler>();
        var creativeFlowHandler = _sp.GetRequiredService<CreativeFlowHandler>();

        var tgUserId = (long)message.From!.Id;

        if (creativeFlowHandler.TryIntercept(tgUserId, out var creativeSession))
        {
            if (message.Text is not null && message.Text.StartsWith("/quit"))
            {
                await chatHandler.HandleQuitAsync(message, ct);
                return;
            }

            if (message.Text is not null && message.Text.StartsWith("/"))
            {
            }
            else
            {
                if (message.MediaGroupId is not null && creativeSession.State == CreativeUserState.AwaitingCreativePost)
                {
                    await botClient.SendMessage(
                        message.Chat.Id,
                        "❌ Media groups (multiple photos/videos) are not supported yet.\n\n" +
                        "Please send a single message with one photo or video.",
                        parseMode: ParseMode.Html,
                        cancellationToken: ct);
                    return;
                }

                await creativeFlowHandler.HandleMessageAsync(message, creativeSession, ct);
                return;
            }
        }

        if (message.Text is not null && message.Text.StartsWith("/chat"))
        {
            await chatHandler.HandleChatCommandAsync(message, ct);
            return;
        }

        if (message.Text is not null && message.Text.StartsWith("/quit"))
        {
            await chatHandler.HandleQuitAsync(message, ct);
            return;
        }

        if (message.Text is null || !message.Text.StartsWith("/"))
        {
            var forwarded = await chatHandler.TryForwardMessageAsync(message, ct);
            if (forwarded) return;
        }

        if (message.Text is null) return;

        if (message.Text.StartsWith("/start"))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "👋 Welcome to Telegram Ads Marketplace!\n" +
                "Use the Mini App to browse channels, create campaigns, and manage your deals.\n" +
                "You'll be able to connect with your counterparty via the /chat command, you'll also receive notifications here about your deals and can approve/reject proposals directly.\n" +
                "Get more info at @Adsmarketplace_showcase",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        else if (message.Text.StartsWith("/help"))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "📚 <b>Commands:</b>\n" +
                "/start - Start the bot\n" +
                "/help - Show this help\n" +
                "/mydeals - View your active deals\n\n" +
                "Use the Mini App for full functionality.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        else if (message.Text.StartsWith("/mydeals"))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "📋 To view your deals, please use the Mini App.\n\n" +
                "You'll receive notifications here when action is required.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery query, CancellationToken ct)
    {
        if (query.Data is null) return;

        var actionRouter = _sp.GetRequiredService<CallbackActionRouter>();
        await actionRouter.RouteAsync(query, ct);
    }
}
