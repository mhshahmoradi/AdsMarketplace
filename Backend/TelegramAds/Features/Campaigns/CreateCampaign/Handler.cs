using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Campaigns.CreateCampaign;

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

    public async Task<CreateCampaignResponse> HandleAsync(CreateCampaignRequest request, CancellationToken ct)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            AdvertiserUserId = _currentUser.UserId,
            Title = request.Title,
            Brief = request.Brief,
            BudgetInTon = request.BudgetInTon,
            MinSubscribers = request.MinSubscribers,
            MinAvgViews = request.MinAvgViews,
            TargetLanguages = request.TargetLanguages,
            ScheduleStart = request.ScheduleStart,
            ScheduleEnd = request.ScheduleEnd,
            Status = CampaignStatus.Open,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);

        return new CreateCampaignResponse(campaign.Id, campaign.Title, campaign.BudgetInTon, campaign.Status.ToString());
    }
}
