using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;

namespace TelegramAds.Features.Campaigns.GetCampaignById;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetCampaignByIdResponse?> HandleAsync(Guid id, CancellationToken ct)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.Applications)
                .ThenInclude(a => a.Channel)
            .Include(c => c.AdvertiserUser)
            .Where(c => c.Id == id)
            .Select(c => new GetCampaignByIdResponse(
                c.Id,
                c.Title,
                c.Brief,
                c.BudgetInTon,
                c.MinSubscribers,
                c.MinAvgViews,
                c.TargetLanguages,
                c.ScheduleStart,
                c.ScheduleEnd,
                c.Status.ToString(),
                c.AdvertiserUserId,
                c.AdvertiserUser.UserName,
                c.AdvertiserUserId == _currentUser.UserId,
                c.Applications.Select(a => new CampaignApplicationItem(
                    a.Id,
                    a.ChannelId,
                    a.Channel.Title,
                    a.Channel.Username,
                    a.ProposedPriceInTon,
                    a.Message,
                    a.Status.ToString(),
                    a.CreatedAt
                )).ToList(),
                c.CreatedAt,
                c.UpdatedAt
            ))
            .FirstOrDefaultAsync(ct);

        return campaign;
    }
}
