namespace TelegramAds.Shared.Db;

public sealed class PaymentWebhook
{
    public Guid Id { get; set; }

    public string InvoiceId { get; set; } = null!;
    public string Payload { get; set; } = null!;

    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public DateTime ReceivedAt { get; set; }
}
