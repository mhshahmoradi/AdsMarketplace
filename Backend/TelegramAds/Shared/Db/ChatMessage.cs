namespace TelegramAds.Shared.Db;

public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid ReceiverUserId { get; set; }
    public long TgMessageId { get; set; }
    public long? ForwardedTgMessageId { get; set; }
    public string? Text { get; set; }
    public bool HasMedia { get; set; }
    public DateTime SentAt { get; set; }

    public Deal Deal { get; set; } = null!;
    public ApplicationUser SenderUser { get; set; } = null!;
    public ApplicationUser ReceiverUser { get; set; } = null!;
}
