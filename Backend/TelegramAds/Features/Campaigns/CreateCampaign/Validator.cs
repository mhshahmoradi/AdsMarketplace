using FluentValidation;

namespace TelegramAds.Features.Campaigns.CreateCampaign;

public sealed class Validator : AbstractValidator<CreateCampaignRequest>
{
    public Validator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Brief).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.BudgetInTon).GreaterThan(0);
        RuleFor(x => x.ScheduleEnd)
            .GreaterThan(x => x.ScheduleStart)
            .When(x => x.ScheduleStart.HasValue && x.ScheduleEnd.HasValue);
    }
}
