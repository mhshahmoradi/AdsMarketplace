namespace TelegramAds.Shared.Db;

public sealed class ChannelStats
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public int SubscriberCount { get; set; }
    public double AvgViewsPerPost { get; set; }
    public double AvgReachPerPost { get; set; }
    public double AvgSharesPerPost { get; set; }
    public double AvgReactionsPerPost { get; set; }
    public double AvgViewsPerStory { get; set; }
    public double AvgSharesPerStory { get; set; }
    public double AvgReactionsPerStory { get; set; }
    public double EnabledNotificationsPercent { get; set; }
    public int FollowersCount { get; set; }
    public int FollowersPrevCount { get; set; }
    public double PremiumSubscriberCount { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? LanguagesGraphJson { get; set; }
    public DateTime StatsStartDate { get; set; }
    public DateTime StatsEndDate { get; set; }
    public DateTime FetchedAt { get; set; }

    public Channel Channel { get; set; } = null!;
}
