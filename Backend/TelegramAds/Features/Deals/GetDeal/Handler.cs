using Microsoft.EntityFrameworkCore;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;

namespace TelegramAds.Features.Deals.GetDeal;

public sealed class Handler
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _currentUser;

    public Handler(AppDbContext db, CurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetDealResponse> HandleAsync(GetDealRequest request, CancellationToken ct)
    {
        var deal = await _db.Deals
            .Include(d => d.Campaign)
            .Include(d => d.Channel)
                .ThenInclude(c => c.Stats)
            .Include(d => d.Listing)
                .ThenInclude(l => l.Channel)
                    .ThenInclude(c => c.Stats)
            .Include(d => d.Proposals)
            .Include(d => d.Events)
            .FirstOrDefaultAsync(d => d.Id == request.DealId, ct);

        if (deal is null)
        {
            throw new AppException(ErrorCodes.NotFound, "Deal not found", 404);
        }

        var channelId = deal.ChannelId ?? deal.Listing?.ChannelId;
        var isParticipant = deal.AdvertiserUserId == _currentUser.UserId ||
                            deal.ChannelOwnerUserId == _currentUser.UserId ||
                            (channelId.HasValue && await _db.ChannelAdmins.AnyAsync(a =>
                                a.ChannelId == channelId.Value &&
                                a.UserId == _currentUser.UserId &&
                                a.CanManageDeals, ct));

        if (!isParticipant)
        {
            throw new AppException(ErrorCodes.Forbidden, "You are not a participant in this deal", 403);
        }

        var proposals = deal.Proposals
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new DealProposalDto(
                p.Id,
                p.ProposerUserId,
                p.ProposedPriceInTon,
                p.ProposedSchedule,
                p.Terms,
                p.Status.ToString(),
                p.CreatedAt
            ))
            .ToList();

        var events = deal.Events
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new DealEventDto(
                e.Id,
                e.FromStatus?.ToString() ?? "",
                e.ToStatus?.ToString() ?? "",
                e.EventType.ToString(),
                e.CreatedAt
            ))
            .ToList();

        var campaign = deal.Campaign != null
            ? new GetDealCampaignDto(
                deal.Campaign.Id,
                deal.Campaign.Title,
                deal.Campaign.Brief,
                deal.Campaign.BudgetInTon,
                deal.Campaign.Status.ToString(),
                deal.Campaign.ScheduleStart,
                deal.Campaign.ScheduleEnd
            )
            : null;

        GetDealListingDto? listing = null;
        if (deal.Listing != null)
        {
            var channelStats = deal.Listing.Channel.Stats != null
                ? new GetDealChannelStatsDto(
                    deal.Listing.Channel.Stats.SubscriberCount,
                    deal.Listing.Channel.Stats.AvgViewsPerPost,
                    deal.Listing.Channel.Stats.AvgReachPerPost,
                    deal.Listing.Channel.Stats.FetchedAt
                )
                : null;

            var channel = new GetDealChannelDto(
                deal.Listing.Channel.Id,
                deal.Listing.Channel.Username,
                deal.Listing.Channel.Title,
                deal.Listing.Channel.Description,
                deal.Listing.Channel.Status.ToString(),
                channelStats
            );

            listing = new GetDealListingDto(
                deal.Listing.Id,
                deal.Listing.Title,
                deal.Listing.Description,
                deal.Listing.Status.ToString(),
                channel
            );
        }
        else if (deal.Channel != null)
        {
            var channelStats = deal.Channel.Stats != null
                ? new GetDealChannelStatsDto(
                    deal.Channel.Stats.SubscriberCount,
                    deal.Channel.Stats.AvgViewsPerPost,
                    deal.Channel.Stats.AvgReachPerPost,
                    deal.Channel.Stats.FetchedAt
                )
                : null;

            var channel = new GetDealChannelDto(
                deal.Channel.Id,
                deal.Channel.Username,
                deal.Channel.Title,
                deal.Channel.Description,
                deal.Channel.Status.ToString(),
                channelStats
            );

            listing = new GetDealListingDto(
                null,
                deal.Channel.Title,
                deal.Channel.Description,
                deal.Channel.Status.ToString(),
                channel
            );
        }

        return new GetDealResponse(
            deal.Id,
            deal.ListingApplicationId.HasValue ? deal.ListingId : null,
            deal.CampaignApplicationId.HasValue ? deal.ChannelId : null,
            deal.CampaignId,
            deal.AdvertiserUserId,
            deal.ChannelOwnerUserId,
            deal.Status.ToString(),
            deal.AgreedPriceInTon,
            deal.AdFormat.ToString(),
            deal.ScheduledPostTime,
            deal.EscrowAddress,
            deal.PostedAt,
            deal.VerifiedAt,
            deal.CreatedAt,
            deal.ExpiresAt,
            deal.CreativeText,
            deal.CreativeRejectionReason,
            campaign,
            listing,
            proposals,
            events
        );
    }
}
