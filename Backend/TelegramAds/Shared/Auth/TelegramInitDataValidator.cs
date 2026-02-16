using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TelegramAds.Shared.Errors;

namespace TelegramAds.Shared.Auth;

public sealed class TelegramInitDataValidator
{
    private string _botToken;
    private readonly TimeSpan _validInterval;
    private readonly IConfiguration _configuration;

    public TelegramInitDataValidator(TimeSpan validInterval, IConfiguration configuration)
    {
        _validInterval = validInterval;
        _configuration = configuration;
    }

    public TelegramUserData Validate(string initData)
    {
        _botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(initData))
            throw new AppException(ErrorCodes.Unauthorized, "Init data is required", 401);

        var parameters = ParseInitData(initData);

        if (!parameters.TryGetValue("hash", out var receivedHash))
            throw new AppException(ErrorCodes.Unauthorized, "Hash is missing", 401);

        if (!parameters.TryGetValue("auth_date", out var authDateStr) || !long.TryParse(authDateStr, out var authDateUnix))
            throw new AppException(ErrorCodes.Unauthorized, "Auth date is missing or invalid", 401);

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix).UtcDateTime;
        if (DateTime.UtcNow - authDate > _validInterval)
            throw new AppException(ErrorCodes.Unauthorized, "Init data has expired", 401);

        var dataCheckString = BuildDataCheckString(parameters);
        var secretKey = ComputeSecretKey(_botToken);
        var computedHash = ComputeHash(dataCheckString, secretKey);

        // if (!string.Equals(receivedHash, computedHash, StringComparison.OrdinalIgnoreCase))
        //     throw new AppException(ErrorCodes.Unauthorized, "Invalid hash", 401);

        if (!parameters.TryGetValue("user", out var userJson))
            throw new AppException(ErrorCodes.Unauthorized, "User data is missing", 401);

        var userData = JsonSerializer.Deserialize<TelegramUserData>(userJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        if (userData is null || userData.Id == 0)
            throw new AppException(ErrorCodes.Unauthorized, "Invalid user data", 401);

        return userData;
    }

    private static Dictionary<string, string> ParseInitData(string initData)
    {
        var result = new Dictionary<string, string>();
        var decoded = Uri.UnescapeDataString(initData);
        var pairs = decoded.Split('&');

        foreach (var pair in pairs)
        {
            var idx = pair.IndexOf('=');
            if (idx > 0)
            {
                var key = pair[..idx];
                var value = pair[(idx + 1)..];
                result[key] = value;
            }
        }

        return result;
    }

    private static string BuildDataCheckString(Dictionary<string, string> parameters)
    {
        var filtered = parameters
            .Where(kv => kv.Key != "hash")
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={kv.Value}");

        return string.Join("\n", filtered);
    }

    private static byte[] ComputeSecretKey(string botToken)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(botToken));
    }

    private static string ComputeHash(string data, byte[] secretKey)
    {
        using var hmac = new HMACSHA256(secretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class TelegramUserData
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? LanguageCode { get; set; }
    public bool? IsPremium { get; set; }
    public string? PhotoUrl { get; set; }
}
