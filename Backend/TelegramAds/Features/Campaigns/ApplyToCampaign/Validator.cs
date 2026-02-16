using FluentValidation;

namespace TelegramAds.Features.Campaigns.ApplyToCampaign;

public sealed class Validator : AbstractValidator<ApplyToCampaignRequest>
{
    public Validator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.ProposedPriceInTon).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
