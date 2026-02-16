using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Campaigns.ApplyToCampaign;

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

    public async Task<ApplyToCampaignResponse> HandleAsync(ApplyToCampaignRequest request, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == request.CampaignId, ct);
        if (campaign is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Campaign not found", 404);
        }

        if (campaign.Status != CampaignStatus.Open)
        {
            throw new AppException(ErrorCodes.InvalidState, "Campaign is not open for applications");
        }

        var channel = await _db.Channels
            .Include(c => c.Stats)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);

        if (channel is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Channel not found", 404);
        }

        if (channel.Status != ChannelStatus.Verified)
        {
            throw new AppException(ErrorCodes.ChannelNotVerified, "Channel must be verified to apply");
        }

        var hasPermission = channel.OwnerUserId == _currentUser.UserId ||
                            await _db.ChannelAdmins.AnyAsync(a =>
                                a.ChannelId == channel.Id &&
                                a.UserId == _currentUser.UserId &&
                                a.CanManageDeals, ct);

        if (!hasPermission)
        {
            throw new AppException(ErrorCodes.Forbidden, "You don't have permission to apply on behalf of this channel", 403);
        }

        var existingApplication = await _db.CampaignApplications
            .AnyAsync(a => a.CampaignId == request.CampaignId && a.ChannelId == request.ChannelId, ct);

        if (existingApplication)
        {
            throw new AppException(ErrorCodes.AlreadyExists, "Application already submitted for this campaign");
        }

        if (campaign.MinSubscribers.HasValue && channel.Stats != null && channel.Stats.SubscriberCount < campaign.MinSubscribers.Value)
        {
            throw new AppException(ErrorCodes.ValidationFailed,
                $"Channel needs at least {campaign.MinSubscribers.Value} subscribers");
        }

        var application = new CampaignApplication
        {
            Id = Guid.NewGuid(),
            CampaignId = request.CampaignId,
            ChannelId = request.ChannelId,
            ApplicantUserId = _currentUser.UserId,
            ProposedPriceInTon = request.ProposedPriceInTon,
            Message = request.Note,
            Status = ApplicationStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        _db.CampaignApplications.Add(application);
        await _db.SaveChangesAsync(ct);

        return new ApplyToCampaignResponse(
            application.Id,
            application.CampaignId,
            application.ChannelId,
            application.ProposedPriceInTon,
            application.Status.ToString()
        );
    }
}
