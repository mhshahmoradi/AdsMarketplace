namespace TelegramAds.Shared.Db;

public sealed class ChannelAdmin
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public long TgUserId { get; set; }
    public Guid? UserId { get; set; }
    public bool CanManageListings { get; set; }
    public bool CanManageDeals { get; set; }
    public DateTime SyncedAt { get; set; }

    public Channel Channel { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
