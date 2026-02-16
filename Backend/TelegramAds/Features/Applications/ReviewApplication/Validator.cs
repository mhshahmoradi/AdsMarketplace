using FluentValidation;

namespace TelegramAds.Features.Applications.ReviewApplication;

public sealed class Validator : AbstractValidator<ReviewApplicationRequest>
{
    private static readonly string[] ValidDecisions = ["Accepted", "Rejected", "Withdrawn"];

    public Validator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty()
            .Must(d => ValidDecisions.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Decision must be one of: Accepted, Rejected, Withdrawn");
    }
}
