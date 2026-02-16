using System.Text.Json.Serialization;

namespace TelegramAds.Shared.Ton;

public sealed record TonApiTransaction(
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("utime")] long Utime,
    [property: JsonPropertyName("in_msg")] TonApiInMessage? InMsg
);

public sealed record TonApiInMessage(
    [property: JsonPropertyName("decoded_op_name")] string? DecodedOpName,
    [property: JsonPropertyName("decoded_body")] TonApiDecodedBody? DecodedBody,
    [property: JsonPropertyName("value")] long Value,
    [property: JsonPropertyName("source")] TonApiAddress? Source
);

public sealed record TonApiDecodedBody(
    [property: JsonPropertyName("text")] string? Text
);

public sealed record TonApiAddress(
    [property: JsonPropertyName("address")] string Address
);

public sealed record TonApiTransactionsResponse(
    [property: JsonPropertyName("transactions")] List<TonApiTransaction> Transactions
);

public sealed class TonApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TonApiClient> _logger;
    private readonly string _baseUrl;

    public TonApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TonApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var apiKey = configuration["Ton:TonApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
    }

    public async Task<List<TonApiTransaction>> GetAccountTransactionsAsync(
        string address,
        int limit = 20,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching transactions for address: {Address}", address);
            var url = $"https://tonapi.io/v2/blockchain/accounts/{address}/transactions?limit=20";

            var response = await _httpClient.GetAsync(
                url,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("TonAPI error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<TonApiTransactionsResponse>(ct);
            return result?.Transactions ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch transactions from TonAPI");
            return [];
        }
    }

    public async Task<TonApiTransaction?> GetTransactionByHashAsync(
        string transactionHash,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching transaction by hash: {Hash}", transactionHash);

            var response = await _httpClient.GetAsync(
                $"https://tonapi.io/v2/blockchain/transactions/{transactionHash}",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TonApiTransaction>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch transaction by hash");
            return null;
        }
    }
}
