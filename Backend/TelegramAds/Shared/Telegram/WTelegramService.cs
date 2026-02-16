using TL;
using WTelegram;

namespace TelegramAds.Shared.Telegram;

public sealed class WTelegramService : IAsyncDisposable
{
    private readonly TelegramOptions _options;
    private Client? _client;
    private bool _isConnected;
    private TaskCompletionSource<string>? _codeCompletionSource;
    private TaskCompletionSource<string>? _passwordCompletionSource;
    private string? _pendingPhoneNumber;
    private bool _waitingForCode;
    private bool _waitingForPassword;
    private Task? _loginTask;
    private readonly IConfiguration _configuration;

    public WTelegramService(TelegramOptions options, IConfiguration configuration)
    {
        _options = options;
        _configuration = configuration;
    }

    public bool IsConnected => _isConnected;
    public bool IsWaitingForCode => _waitingForCode;
    public bool IsWaitingForPassword => _waitingForPassword;

    private string? ConfigCallback(string what)
    {
        return what switch
        {
            "api_id" => _configuration["Telegram:ApiId"],
            "api_hash" => _configuration["Telegram:ApiHash"],
            "phone_number" => "+17016451204",
            "verification_code" => WaitForInput(ref _codeCompletionSource, ref _waitingForCode),
            "password" => WaitForInput(ref _passwordCompletionSource, ref _waitingForPassword),
            _ => null
        };
    }

    private static string? WaitForInput(ref TaskCompletionSource<string>? tcs, ref bool flag)
    {
        tcs = new TaskCompletionSource<string>();
        flag = true;
        return tcs.Task.GetAwaiter().GetResult();
    }

    public async Task<LoginResult> StartLoginAsync(string phoneNumber, string? code = null)
    {
        if (_isConnected)
            return new LoginResult(true, "Already logged in", LoginStep.Complete);

        if (code != null && _waitingForCode && _codeCompletionSource is { Task.IsCompleted: false })
        {
            _codeCompletionSource.TrySetResult(code);
            _waitingForCode = false;
            await Task.Delay(3000);

            if (_isConnected)
                return new LoginResult(true, "Login successful", LoginStep.Complete);

            if (_waitingForPassword)
                return new LoginResult(false, "2FA password required", LoginStep.WaitingForPassword);

            return new LoginResult(false, "Code verification failed", LoginStep.Error);
        }

        _pendingPhoneNumber = phoneNumber;
        _codeCompletionSource = null;
        _passwordCompletionSource = null;
        _waitingForCode = false;
        _waitingForPassword = false;

        _client?.Dispose();
        _client = new Client(ConfigCallback);

        _loginTask = Task.Run(async () =>
        {
            try
            {
                var user = await _client.LoginUserIfNeeded();
                _isConnected = user != null;
            }
            catch
            {
                _isConnected = false;
            }
        });

        await Task.Delay(2000);

        if (_isConnected)
            return new LoginResult(true, "Logged in from saved session", LoginStep.Complete);

        if (_waitingForCode)
            return new LoginResult(false, "Verification code sent to your Telegram", LoginStep.WaitingForCode);

        return new LoginResult(false, "Login in progress", LoginStep.WaitingForCode);
    }

    public async Task<LoginResult> SubmitPasswordAsync(string password)
    {
        if (_passwordCompletionSource is null || _passwordCompletionSource.Task.IsCompleted)
            return new LoginResult(false, "No pending password request", LoginStep.Error);

        _passwordCompletionSource.TrySetResult(password);
        _waitingForPassword = false;
        await Task.Delay(3000);

        if (_isConnected)
            return new LoginResult(true, "Login successful", LoginStep.Complete);

        return new LoginResult(false, "Password verification failed", LoginStep.Error);
    }

    public async Task EnsureConnectedAsync()
    {
        if (_isConnected && _client != null) return;

        _client = new Client(ConfigCallback);
        var user = await _client.LoginUserIfNeeded();
        _isConnected = user != null;

        if (!_isConnected)
            throw new InvalidOperationException("Not logged in. Use the login API first.");
    }

    public async Task<BroadcastStatsResult?> GetChannelStatsAsync(string username)
    {
        await EnsureConnectedAsync();

        var resolved = await _client!.Contacts_ResolveUsername(username);
        var channel = resolved.Chat as Channel;

        if (channel == null)
            throw new InvalidOperationException("Could not resolve channel");

        var inputChannel = new InputChannel(channel.id, channel.access_hash);

        var stats = await _client.Stats_GetBroadcastStats(inputChannel);
        var statgraph = await _client.Stats_LoadAsyncGraph(((StatsGraphAsync)stats.languages_graph).token) as StatsGraph;
        if (stats == null)
            return null;

        string? topLanguage = null;
        string? languagesGraphJson = null;
        if (statgraph != null)
        {
            languagesGraphJson = statgraph.json.data;
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(languagesGraphJson);
                if (doc.RootElement.TryGetProperty("names", out var names))
                {
                    var firstProperty = names.EnumerateObject().FirstOrDefault();
                    if (firstProperty.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        topLanguage = firstProperty.Value.GetString();
                    }
                }
            }
            catch { }
        }

        return new BroadcastStatsResult
        {
            PeriodStart = stats.period.min_date,
            PeriodEnd = stats.period.max_date,
            Followers = MapAbsValue(stats.followers),
            ViewsPerPost = MapAbsValue(stats.views_per_post),
            SharesPerPost = MapAbsValue(stats.shares_per_post),
            ReactionsPerPost = MapAbsValue(stats.reactions_per_post),
            ViewsPerStory = MapAbsValue(stats.views_per_story),
            SharesPerStory = MapAbsValue(stats.shares_per_story),
            ReactionsPerStory = MapAbsValue(stats.reactions_per_story),
            EnabledNotificationsPercent = stats.enabled_notifications.total > 0
                ? stats.enabled_notifications.part / (double)stats.enabled_notifications.total * 100
                : 0,
            TopLanguage = topLanguage,
            LanguagesGraphJson = languagesGraphJson
        };
    }

    private static StatsValue MapAbsValue(StatsAbsValueAndPrev val)
    {
        return new StatsValue { Current = val.current, Previous = val.previous };
    }

    public async Task<BoostStatusResult?> GetBoostStatusAsync(string username)
    {
        await EnsureConnectedAsync();

        var resolved = await _client!.Contacts_ResolveUsername(username);
        var channel = resolved.Chat as Channel;

        if (channel == null)
            throw new InvalidOperationException("Could not resolve channel");

        var inputPeer = new InputPeerChannel(channel.id, channel.access_hash);

        try
        {
            var boostStatus = await _client.Premium_GetBoostsStatus(inputPeer);
            if (boostStatus == null)
                return null;

            return new BoostStatusResult
            {
                Level = boostStatus.level,
                CurrentLevelBoosts = boostStatus.current_level_boosts,
                Boosts = boostStatus.boosts,
                PremiumSubscribers = (int)(boostStatus.premium_audience?.total ?? 0),
                PremiumAudiencePercent = boostStatus.premium_audience?.part ?? 0
            };
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}

public sealed class ChannelInfo
{
    public long ChannelId { get; set; }
    public long AccessHash { get; set; }
    public string Title { get; set; } = "";
    public string? Username { get; set; }
    public int SubscriberCount { get; set; }
    public string? Description { get; set; }
}

public sealed class BroadcastStatsResult
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public StatsValue Followers { get; set; } = new();
    public StatsValue ViewsPerPost { get; set; } = new();
    public StatsValue SharesPerPost { get; set; } = new();
    public StatsValue ReactionsPerPost { get; set; } = new();
    public StatsValue ViewsPerStory { get; set; } = new();
    public StatsValue SharesPerStory { get; set; } = new();
    public StatsValue ReactionsPerStory { get; set; } = new();
    public double EnabledNotificationsPercent { get; set; }
    public string? TopLanguage { get; set; }
    public string? LanguagesGraphJson { get; set; }
}

public sealed class StatsValue
{
    public double Current { get; set; }
    public double Previous { get; set; }
}

public sealed class BoostStatusResult
{
    public int Level { get; set; }
    public int CurrentLevelBoosts { get; set; }
    public int Boosts { get; set; }
    public int PremiumSubscribers { get; set; }
    public double PremiumAudiencePercent { get; set; }
}

public sealed record LoginResult(bool Success, string Message, LoginStep Step);

public enum LoginStep
{
    WaitingForCode,
    WaitingForPassword,
    Complete,
    Error
}
