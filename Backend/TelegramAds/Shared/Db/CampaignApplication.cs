namespace TelegramAds.Shared.Db;

public enum ApplicationStatus
{
    Pending,
    Accepted,
    Rejected,
    Withdrawn
}

public sealed class CampaignApplication
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public decimal ProposedPriceInTon { get; set; }
    public string? Message { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
    public ApplicationUser ApplicantUser { get; set; } = null!;
}
