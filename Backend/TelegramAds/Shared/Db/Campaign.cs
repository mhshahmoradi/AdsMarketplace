namespace TelegramAds.Shared.Db;

public enum CampaignStatus
{
    Draft,
    Open,
    InProgress,
    Completed,
    Cancelled
}

public sealed class Campaign
{
    public Guid Id { get; set; }
    public Guid AdvertiserUserId { get; set; }
    public string Title { get; set; } = null!;
    public string Brief { get; set; } = null!;
    public decimal BudgetInTon { get; set; }
    public int? MinSubscribers { get; set; }
    public int? MinAvgViews { get; set; }
    public string? TargetLanguages { get; set; }
    public DateTime? ScheduleStart { get; set; }
    public DateTime? ScheduleEnd { get; set; }
    public CampaignStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser AdvertiserUser { get; set; } = null!;
    public ICollection<CampaignApplication> Applications { get; set; } = [];
}
