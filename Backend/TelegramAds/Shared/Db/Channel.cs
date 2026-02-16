namespace TelegramAds.Shared.Db;

public enum ChannelStatus
{
    Pending,
    Verified,
    Suspended
}

public sealed class Channel
{
    public Guid Id { get; set; }
    public long TgChannelId { get; set; }
    public string Username { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Topic { get; set; }
    public ChannelStatus Status { get; set; }
    public Guid OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser OwnerUser { get; set; } = null!;
    public ICollection<ChannelAdmin> Admins { get; set; } = [];
    public ChannelStats? Stats { get; set; }
    public ICollection<Listing> Listings { get; set; } = [];
}
