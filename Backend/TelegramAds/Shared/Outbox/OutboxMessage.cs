namespace TelegramAds.Shared.Outbox;

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public OutboxMessageStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}
