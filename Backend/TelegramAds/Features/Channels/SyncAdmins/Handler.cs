using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Channels.SyncAdmins;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly ITelegramBotClient _bot;
    private readonly IClock _clock;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, ITelegramBotClient bot, IClock clock, CurrentUser currentUser)
    {
        _db = db;
        _bot = bot;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<SyncAdminsResponse> HandleAsync(SyncAdminsRequest request, CancellationToken ct)
    {
        var channel = await _db.Channels
            .Include(c => c.Admins)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);

        if (channel is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Channel not found", 404);
        }

        if (channel.OwnerUserId != _currentUser.UserId)
        {
            throw new AppException(ErrorCodes.Forbidden, "Only the channel owner can sync admins", 403);
        }

        var chatAdmins = await _bot.SendRequest(new GetChatAdministratorsRequest { ChatId = new ChatId(channel.TgChannelId) }, ct);

        var telegramAdminIds = chatAdmins
            .Select(a => a.User.Id)
            .ToHashSet();

        var existingAdminTgIds = channel.Admins
            .Select(a => a.TgUserId)
            .ToHashSet();

        int addedCount = 0;

        var newAdminTgIds = chatAdmins
            .Where(a => !existingAdminTgIds.Contains(a.User.Id))
            .Select(a => a.User.Id)
            .ToList();

        foreach (var tgId in newAdminTgIds)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TgUserId == tgId, ct);

            var newAdmin = new ChannelAdmin
            {
                Id = Guid.NewGuid(),
                ChannelId = channel.Id,
                TgUserId = tgId,
                UserId = user?.Id,
                CanManageListings = false,
                CanManageDeals = false,
                SyncedAt = _clock.UtcNow
            };
            _db.ChannelAdmins.Add(newAdmin);
            addedCount++;
        }

        var adminsToRemove = channel.Admins.Where(a => !telegramAdminIds.Contains(a.TgUserId)).ToList();
        _db.ChannelAdmins.RemoveRange(adminsToRemove);
        var removedCount = adminsToRemove.Count;

        await _db.SaveChangesAsync(ct);

        return new SyncAdminsResponse(
            channel.Id,
            telegramAdminIds.Count,
            addedCount,
            removedCount
        );
    }
}
