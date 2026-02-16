namespace TelegramAds.Shared.Db;

public sealed class ListingApplication
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public decimal ProposedPriceInTon { get; set; }
    public string? Message { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public Listing Listing { get; set; } = null!;
    public Campaign Campaign { get; set; } = null!;
    public ApplicationUser ApplicantUser { get; set; } = null!;
}
