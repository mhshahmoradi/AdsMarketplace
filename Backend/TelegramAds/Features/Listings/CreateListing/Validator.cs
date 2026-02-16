using FluentValidation;

namespace TelegramAds.Features.Listings.CreateListing;

public sealed class Validator : AbstractValidator<CreateListingRequest>
{
    public Validator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.AdFormats).NotEmpty().WithMessage("At least one ad format is required");
        RuleForEach(x => x.AdFormats).ChildRules(format =>
        {
            format.RuleFor(f => f.PriceInTon).GreaterThan(0);
        });
    }
}
