using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramAds.Features.Bot.Templates;

public static class MessageTemplates
{
    public static string NewProposalNotification(string channelTitle, decimal price, DateTime? schedule) =>
        $"📩 <b>New Proposal Received</b>\n\n" +
        $"Channel: {channelTitle}\n" +
        $"Price: {price} TON\n" +
        (schedule.HasValue ? $"Schedule: {schedule:g}\n" : "") +
        "\nUse the buttons below to respond.";

    public static string ProposalAcceptedNotification(string channelTitle, decimal price) =>
        $"✅ <b>Proposal Accepted!</b>\n\n" +
        $"Channel: {channelTitle}\n" +
        $"Agreed Price: {price} TON\n\n" +
        "Please proceed with payment to the escrow address.";

    public static string ProposalRejectedNotification(string channelTitle) =>
        $"❌ <b>Proposal Rejected</b>\n\n" +
        $"Channel: {channelTitle}\n\n" +
        "The other party has rejected your proposal.";

    public static string PaymentReceivedNotification(Guid dealId, decimal amount) =>
        $"💰 <b>Payment Received</b>\n\n" +
        $"Amount: {amount} TON\n" +
        $"Deal: {dealId:N}\n\n" +
        "Channel owner can now submit creative for approval.";

    public static string CreativeSubmittedNotification(string channelTitle, string creativePreview) =>
        $"🎨 <b>Creative Submitted for Review</b>\n\n" +
        $"Channel: {channelTitle}\n\n" +
        $"<i>{TruncateText(creativePreview, 200)}</i>\n\n" +
        "Please review and approve or request changes.";

    public static string CreativeApprovedNotification(string channelTitle) =>
        $"✅ <b>Creative Approved!</b>\n\n" +
        $"Channel: {channelTitle}\n\n" +
        "Your creative has been approved. Please set a schedule for the post.";

    public static string CreativeEditsRequestedNotification(string channelTitle, string reason) =>
        $"✏️ <b>Edits Requested</b>\n\n" +
        $"Channel: {channelTitle}\n\n" +
        $"Reason: {reason}\n\n" +
        "Please update your creative and resubmit.";

    public static string PostScheduledNotification(string channelTitle, DateTime scheduledTime) =>
        $"📅 <b>Post Scheduled</b>\n\n" +
        $"Channel: {channelTitle}\n" +
        $"Time: {scheduledTime:g} UTC\n\n" +
        "The ad will be automatically posted at the scheduled time.";

    public static string PostPublishedNotification(string channelTitle, string postUrl) =>
        $"📢 <b>Ad Published!</b>\n\n" +
        $"Channel: {channelTitle}\n" +
        $"Post: {postUrl}\n\n" +
        "Verification period started. Funds will be released after successful verification.";

    public static string PostVerifiedNotification(string channelTitle) =>
        $"✅ <b>Post Verified</b>\n\n" +
        $"Channel: {channelTitle}\n\n" +
        "The post has been verified. Funds will be released shortly.";

    public static string FundsReleasedNotification(decimal amount, string channelTitle) =>
        $"💸 <b>Funds Released</b>\n\n" +
        $"Amount: {amount} TON\n" +
        $"Channel: {channelTitle}\n\n" +
        "The deal has been completed successfully!";

    public static string FundsRefundedNotification(decimal amount, string channelTitle, string reason) =>
        $"↩️ <b>Funds Refunded</b>\n\n" +
        $"Amount: {amount} TON\n" +
        $"Channel: {channelTitle}\n" +
        $"Reason: {reason}\n\n" +
        "The funds have been returned to your wallet.";

    public static string DealCreatedNotification(string channelTitle, decimal price, Guid dealId) =>
        $"🤝 <b>New Deal Created</b>\n\n" +
        $"Channel: {channelTitle}\n" +
        $"Price: {price} TON\n" +
        $"Deal ID: {dealId:N}\n\n" +
        "The deal is now in negotiation phase.";

    public static string DealCancelledNotification(string channelTitle, string reason) =>
        $"🚫 <b>Deal Cancelled</b>\n\n" +
        $"Channel: {channelTitle}\n" +
        $"Reason: {reason}\n\n" +
        "The deal has been cancelled.";

    public static string DealExpiredNotification(string channelTitle) =>
        $"⏰ <b>Deal Expired</b>\n\n" +
        $"Channel: {channelTitle}\n\n" +
        "The deal has expired due to inactivity.";

    public static InlineKeyboardMarkup ProposalActionKeyboard(Guid proposalId) =>
        new(
        [
            [
                InlineKeyboardButton.WithCallbackData("✅ Accept", $"accept_proposal:{proposalId}"),
                InlineKeyboardButton.WithCallbackData("❌ Reject", $"reject_proposal:{proposalId}")
            ]
        ]);

    public static InlineKeyboardMarkup CreativeReviewKeyboard(Guid dealId) =>
        new(
        [
            [
                InlineKeyboardButton.WithCallbackData("✅ Approve", $"approve_creative:{dealId}"),
                InlineKeyboardButton.WithCallbackData("✏️ Request Edits", $"request_edits:{dealId}")
            ]
        ]);

    private static string TruncateText(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
}
