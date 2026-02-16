using FluentValidation;

namespace TelegramAds.Features.Deals.ReviewCreative;

public sealed class Validator : AbstractValidator<ReviewCreativeRequest>
{
    public Validator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty()
            .Must(d => d.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ||
                      d.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Decision must be 'Accepted' or 'Rejected'");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => x.Decision.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Rejection reason is required when rejecting creative");

        RuleFor(x => x.RejectionReason)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.RejectionReason));
    }
}
