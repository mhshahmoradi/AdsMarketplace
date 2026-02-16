using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Time;

namespace TelegramAds.Features.Applications.ReviewApplication;

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

    public async Task<ReviewApplicationResponse> HandleAsync(Guid applicationId, ReviewApplicationRequest request, CancellationToken ct)
    {
        var campaignApp = await _db.CampaignApplications
            .Include(a => a.Campaign)
            .Include(a => a.Channel).ThenInclude(c => c.Listings).ThenInclude(l => l.AdFormats)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (campaignApp is not null)
        {
            return await HandleCampaignApplicationAsync(campaignApp, request, ct);
        }

        var listingApp = await _db.ListingApplications
            .Include(a => a.Listing).ThenInclude(l => l.Channel)
            .Include(a => a.Listing).ThenInclude(l => l.AdFormats)
            .Include(a => a.Campaign)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (listingApp is not null)
        {
            return await HandleListingApplicationAsync(listingApp, request, ct);
        }

        throw new AppException(ErrorCodes.NotFound, "Application not found", 404);
    }

    private async Task<ReviewApplicationResponse> HandleCampaignApplicationAsync(
        CampaignApplication application,
        ReviewApplicationRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ApplicationStatus>(request.Decision, ignoreCase: true, out var newStatus))
        {
            throw new AppException(ErrorCodes.ValidationFailed, $"Invalid decision: {request.Decision}");
        }

        ValidateCampaignApplicationPermissions(application, newStatus);
        ValidateStatusTransition(application.Status, newStatus);

        application.Status = newStatus;
        application.RespondedAt = _clock.UtcNow;

        Guid? dealId = null;
        if (newStatus == ApplicationStatus.Accepted)
        {
            var deal = CreateDealFromCampaignApplication(application);
            _db.Deals.Add(deal);
            dealId = deal.Id;
        }

        await _db.SaveChangesAsync(ct);

        return new ReviewApplicationResponse(application.Id, newStatus.ToString(), dealId, application.RespondedAt ?? _clock.UtcNow);
    }

    private async Task<ReviewApplicationResponse> HandleListingApplicationAsync(
        ListingApplication application,
        ReviewApplicationRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ApplicationStatus>(request.Decision, ignoreCase: true, out var newStatus))
        {
            throw new AppException(ErrorCodes.ValidationFailed, $"Invalid decision: {request.Decision}");
        }

        ValidateListingApplicationPermissions(application, newStatus);
        ValidateStatusTransition(application.Status, newStatus);

        application.Status = newStatus;
        application.RespondedAt = _clock.UtcNow;

        Guid? dealId = null;
        if (newStatus == ApplicationStatus.Accepted)
        {
            var deal = CreateDealFromListingApplication(application);
            _db.Deals.Add(deal);
            dealId = deal.Id;
        }

        await _db.SaveChangesAsync(ct);

        return new ReviewApplicationResponse(application.Id, newStatus.ToString(), dealId, application.RespondedAt ?? _clock.UtcNow);
    }

    private void ValidateCampaignApplicationPermissions(CampaignApplication application, ApplicationStatus newStatus)
    {
        var isCampaignOwner = application.Campaign.AdvertiserUserId == _currentUser.UserId;
        var isApplicant = application.ApplicantUserId == _currentUser.UserId;

        if (newStatus == ApplicationStatus.Withdrawn)
        {
            if (!isApplicant)
            {
                throw new AppException(ErrorCodes.Forbidden, "Only the applicant can withdraw an application", 403);
            }
        }
        else
        {
            if (!isCampaignOwner)
            {
                throw new AppException(ErrorCodes.Forbidden, "Only the campaign owner can accept or reject applications", 403);
            }
        }
    }

    private void ValidateListingApplicationPermissions(ListingApplication application, ApplicationStatus newStatus)
    {
        var isListingOwner = application.Listing.Channel.OwnerUserId == _currentUser.UserId;
        var isApplicant = application.ApplicantUserId == _currentUser.UserId;

        if (newStatus == ApplicationStatus.Withdrawn)
        {
            if (!isApplicant)
            {
                throw new AppException(ErrorCodes.Forbidden, "Only the applicant can withdraw an application", 403);
            }
        }
        else
        {
            if (!isListingOwner)
            {
                throw new AppException(ErrorCodes.Forbidden, "Only the listing owner can accept or reject applications", 403);
            }
        }
    }

    private static void ValidateStatusTransition(ApplicationStatus currentStatus, ApplicationStatus newStatus)
    {
        if (currentStatus != ApplicationStatus.Pending)
        {
            throw new AppException(ErrorCodes.InvalidState, $"Cannot change application status from {currentStatus}");
        }
    }

    private Deal CreateDealFromCampaignApplication(CampaignApplication application)
    {
        var scheduledTime = application.Campaign.ScheduleStart ?? _clock.UtcNow.AddHours(1);

        return new Deal
        {
            Id = Guid.NewGuid(),
            ChannelId = application.ChannelId,
            AdvertiserUserId = application.Campaign.AdvertiserUserId,
            ChannelOwnerUserId = application.Channel.OwnerUserId,
            CampaignId = application.CampaignId,
            CampaignApplicationId = application.Id,
            ListingApplicationId = null,
            Status = DealStatus.Agreed,
            AgreedPriceInTon = application.ProposedPriceInTon,
            AdFormat = AdFormatType.Post,
            ScheduledPostTime = scheduledTime,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
    }

    private Deal CreateDealFromListingApplication(ListingApplication application)
    {
        var adFormat = application.Listing.AdFormats.FirstOrDefault();

        var scheduledTime = application.Campaign.ScheduleStart ?? _clock.UtcNow.AddHours(1);

        return new Deal
        {
            Id = Guid.NewGuid(),
            ListingId = application.ListingId,
            ChannelId = application.Listing.ChannelId,
            AdvertiserUserId = application.Campaign.AdvertiserUserId,
            ChannelOwnerUserId = application.Listing.Channel.OwnerUserId,
            CampaignId = application.CampaignId,
            CampaignApplicationId = null,
            ListingApplicationId = application.Id,
            Status = DealStatus.Agreed,
            AgreedPriceInTon = application.ProposedPriceInTon,
            AdFormat = adFormat?.FormatType ?? AdFormatType.Post,
            ScheduledPostTime = scheduledTime,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
    }
}
