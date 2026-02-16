using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Campaigns.UpdateCampaign;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, IClock clock, CurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<UpdateCampaignResponse?> HandleAsync(Guid campaignId, UpdateCampaignRequest request, CancellationToken ct)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.AdvertiserUserId == _currentUser.UserId, ct);

        if (campaign == null)
            return null;

        if (request.Title != null)
            campaign.Title = request.Title;

        if (request.Brief != null)
            campaign.Brief = request.Brief;

        if (request.BudgetInTon.HasValue)
            campaign.BudgetInTon = request.BudgetInTon.Value;

        if (request.MinSubscribers.HasValue)
            campaign.MinSubscribers = request.MinSubscribers.Value;

        if (request.MinAvgViews.HasValue)
            campaign.MinAvgViews = request.MinAvgViews.Value;

        if (request.TargetLanguages != null)
            campaign.TargetLanguages = request.TargetLanguages;

        if (request.ScheduleStart.HasValue)
            campaign.ScheduleStart = request.ScheduleStart.Value;

        if (request.ScheduleEnd.HasValue)
            campaign.ScheduleEnd = request.ScheduleEnd.Value;

        campaign.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new UpdateCampaignResponse(
            campaign.Id,
            campaign.Title,
            campaign.Brief,
            campaign.BudgetInTon,
            campaign.MinSubscribers,
            campaign.MinAvgViews,
            campaign.TargetLanguages,
            campaign.ScheduleStart,
            campaign.ScheduleEnd,
            campaign.Status.ToString(),
            campaign.UpdatedAt
        );
    }
}
