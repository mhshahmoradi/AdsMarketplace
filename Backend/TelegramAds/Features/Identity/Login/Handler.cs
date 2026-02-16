using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Identity.Login;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly TelegramInitDataValidator _validator;
    private readonly JwtService _jwtService;
    private readonly IClock _clock;

    public Handler(AppDbContext db, TelegramInitDataValidator validator, JwtService jwtService, IClock clock)
    {
        _db = db;
        _validator = validator;
        _jwtService = jwtService;
        _clock = clock;
    }

    public async Task<LoginResponse> HandleAsync(string initData, CancellationToken ct)
    {
        var tgUser = _validator.Validate(initData);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.TgUserId == tgUser.Id, ct);
        var isNewUser = user is null;

        if (isNewUser)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                TgUserId = tgUser.Id,
                TgUsername = tgUser.Username,
                FirstName = tgUser.FirstName,
                LastName = tgUser.LastName,
                PhotoUrl = tgUser.PhotoUrl,
                UserName = tgUser.Username ?? $"tg_{tgUser.Id}",
                CreatedAt = _clock.UtcNow,
                LastLoginAt = _clock.UtcNow
            };
            _db.Users.Add(user);
        }
        else
        {
            user!.TgUsername = tgUser.Username;
            user.FirstName = tgUser.FirstName;
            user.LastName = tgUser.LastName;
            user.PhotoUrl = tgUser.PhotoUrl;
            user.LastLoginAt = _clock.UtcNow;
        }

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == user.Id, ct);
        if (wallet is null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Balance = 0,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow
            };
            _db.Wallets.Add(wallet);
        }

        await _db.SaveChangesAsync(ct);

        var token = _jwtService.GenerateToken(user.Id, user.TgUserId, user.TgUsername, user.FirstName);

        return new LoginResponse(
            token,
            user.Id,
            user.TgUserId,
            user.TgUsername,
            user.FirstName,
            user.LastName,
            isNewUser
        );
    }
}
