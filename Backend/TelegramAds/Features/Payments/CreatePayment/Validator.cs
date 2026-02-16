using FluentValidation;

namespace TelegramAds.Features.Payments.CreatePayment;

public sealed class Validator : AbstractValidator<CreatePaymentRequest>
{
    public Validator()
    {
        RuleFor(x => x.DealId)
            .NotEmpty()
            .WithMessage("Deal ID is required");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Must(c => c == "TON" || c == "USDT")
            .WithMessage("Currency must be TON or USDT");
    }
}
