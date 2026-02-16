using FluentValidation;

namespace TelegramAds.Features.Deals.SubmitCreative;

public sealed class Validator : AbstractValidator<SubmitCreativeRequest>
{
    public Validator()
    {
        RuleFor(x => x.CreativeText)
            .NotEmpty()
            .MaximumLength(4000)
            .WithMessage("Creative text must be between 1 and 4000 characters");
    }
}
