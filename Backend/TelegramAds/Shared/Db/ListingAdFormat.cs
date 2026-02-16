namespace TelegramAds.Shared.Db;

public enum AdFormatType
{
    Post,
    Story,
    Pinned,
    Repost
}

public sealed class ListingAdFormat
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public AdFormatType FormatType { get; set; }
    public decimal PriceInTon { get; set; }
    public int? DurationHours { get; set; }
    public string? Terms { get; set; }

    public Listing Listing { get; set; } = null!;
}
