namespace TelegramAds.Shared.Db;

public enum ProposalStatus
{
    Pending,
    Accepted,
    Rejected,
    Superseded
}

public sealed class DealProposal
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Guid ProposerUserId { get; set; }
    public decimal ProposedPriceInTon { get; set; }
    public DateTime? ProposedSchedule { get; set; }
    public string? Terms { get; set; }
    public ProposalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public Deal Deal { get; set; } = null!;
    public ApplicationUser ProposerUser { get; set; } = null!;
}
