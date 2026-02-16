namespace TelegramAds.Shared.Errors;

public sealed class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public AppException(string code, string message, int statusCode = 400) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InvalidState = "INVALID_STATE";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string BotNotAdmin = "BOT_NOT_ADMIN";
    public const string UserNotAdmin = "USER_NOT_ADMIN";
    public const string ChannelNotVerified = "CHANNEL_NOT_VERIFIED";
    public const string DealStateInvalid = "DEAL_STATE_INVALID";
    public const string PaymentFailed = "PAYMENT_FAILED";
    public const string TelegramError = "TELEGRAM_ERROR";
    public const string AlreadyExists = "ALREADY_EXISTS";
}
