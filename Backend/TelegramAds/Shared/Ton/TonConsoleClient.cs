using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelegramAds.Shared.Ton;

public sealed record CreateInvoiceRequest(
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("life_time")] int LifeTime
);

public sealed record CreateInvoiceResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("payment_link")] string PaymentLink,
    [property: JsonPropertyName("pay_to_address")] string PayToAddress,
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("date_create")] long DateCreate,
    [property: JsonPropertyName("date_expire")] long DateExpire
);

public sealed record TonConsoleWebhookPayload(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("date_create")] long DateCreate,
    [property: JsonPropertyName("date_expire")] long DateExpire,
    [property: JsonPropertyName("date_change")] long DateChange,
    [property: JsonPropertyName("payment_link")] string PaymentLink,
    [property: JsonPropertyName("pay_to_address")] string PayToAddress,
    [property: JsonPropertyName("paid_by_address")] string? PaidByAddress,
    [property: JsonPropertyName("overpayment")] string? Overpayment
);

public sealed class TonConsoleClient
{
    private readonly HttpClient _httpClient;
    private readonly TonConsoleOptions _options;
    private readonly ILogger<TonConsoleClient> _logger;

    public TonConsoleClient(
        HttpClient httpClient,
        TonConsoleOptions options,
        ILogger<TonConsoleClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
    }

    public async Task<CreateInvoiceResponse> CreateInvoiceAsync(
        decimal amountInTon,
        string currency,
        string description,
        CancellationToken ct = default)
    {
        var amountInNanoTon = ((long)(amountInTon * 1_000_000_000)).ToString();

        var request = new CreateInvoiceRequest(
            Amount: amountInNanoTon,
            Currency: currency,
            Description: description,
            LifeTime: _options.DefaultInvoiceLifetimeSeconds
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Creating TON Console invoice: Amount={Amount} TON, Currency={Currency}",
            amountInTon, currency);

        var response = await _httpClient.PostAsync("/services/invoices/invoice", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Failed to create TON Console invoice: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new HttpRequestException(
                $"TON Console API error: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var invoice = JsonSerializer.Deserialize<CreateInvoiceResponse>(responseJson)
            ?? throw new InvalidOperationException("Failed to deserialize invoice response");

        _logger.LogInformation("TON Console invoice created: InvoiceId={InvoiceId}, PaymentLink={PaymentLink}",
            invoice.Id, invoice.PaymentLink);

        return invoice;
    }

    public async Task<TonConsoleWebhookPayload?> GetInvoiceStatusAsync(
        string invoiceId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching TON Console invoice status: InvoiceId={InvoiceId}", invoiceId);

        var response = await _httpClient.GetAsync($"/services/invoices/invoice/{invoiceId}", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch invoice status: {StatusCode}", response.StatusCode);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TonConsoleWebhookPayload>(responseJson);
    }
}
