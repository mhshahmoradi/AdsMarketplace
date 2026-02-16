namespace TelegramAds.Shared.Db;

public enum ListingStatus
{
    Draft,
    Published,
    Paused,
    Archived
}

public sealed class Listing
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public ListingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Channel Channel { get; set; } = null!;
    public ICollection<ListingAdFormat> AdFormats { get; set; } = [];
}
