using FluentValidation;

namespace TelegramAds.Features.Channels.AddChannel;

public sealed class Validator : AbstractValidator<AddChannelRequest>
{
    public Validator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(128)
            .Matches(@"^@?[a-zA-Z][a-zA-Z0-9_]{3,30}$")
            .WithMessage("Invalid channel username format");
    }
}
