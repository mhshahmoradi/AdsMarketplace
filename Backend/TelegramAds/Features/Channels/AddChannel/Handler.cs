using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Channels.AddChannel;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly ITelegramBotClient _botClient;
    private readonly IClock _clock;
    private readonly CurrentUser _currentUser;

    public Handler(
        AppDbContext db,
        ITelegramBotClient botClient,
        IClock clock,
        CurrentUser currentUser)
    {
        _db = db;
        _botClient = botClient;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<AddChannelResponse> HandleAsync(AddChannelRequest request, CancellationToken ct)
    {
        var username = request.Username.TrimStart('@');

        var existing = await _db.Channels.FirstOrDefaultAsync(c => c.Username == username, ct);
        if (existing is not null)
        {
            throw new AppException(ErrorCodes.ValidationFailed, "Channel already registered");
        }

        var chatId = "@" + username;

        Telegram.Bot.Types.ChatFullInfo chat;
        try
        {
            chat = await _botClient.GetChat(chatId, ct);
        }
        catch (Exception ex)
        {
            throw new AppException(ErrorCodes.TelegramError, $"Could not fetch channel data: {ex.Message}");
        }

        int memberCount;
        try
        {
            memberCount = await _botClient.GetChatMemberCount(chatId, ct);
        }
        catch
        {
            memberCount = 0;
        }

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            TgChannelId = chat.Id,
            Username = username,
            Title = chat.Title ?? username,
            Description = chat.Description,
            Status = ChannelStatus.Pending,
            OwnerUserId = _currentUser.UserId,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };

        var stats = new ChannelStats
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id,
            SubscriberCount = memberCount,
            FetchedAt = _clock.UtcNow
        };

        channel.Stats = stats;

        _db.Channels.Add(channel);
        await _db.SaveChangesAsync(ct);

        return new AddChannelResponse(
            channel.Id,
            channel.TgChannelId,
            channel.Username,
            channel.Title,
            channel.Description,
            channel.Status.ToString(),
            stats.SubscriberCount
        );
    }
}
