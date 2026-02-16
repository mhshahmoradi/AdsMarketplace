using FluentValidation;

namespace TelegramAds.Features.Listings.ApplyToListing;

public sealed class Validator : AbstractValidator<ApplyToListingRequest>
{
    public Validator()
    {
        RuleFor(x => x.ListingId).NotEmpty();
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.ProposedPriceInTon).GreaterThan(0);
        RuleFor(x => x.Message).MaximumLength(2000);
    }
}
