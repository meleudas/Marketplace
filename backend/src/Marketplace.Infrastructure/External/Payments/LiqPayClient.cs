using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.Application.Payments.Ports;
using Marketplace.Infrastructure.Observability;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.External.Payments;

public sealed class LiqPayClient : ILiqPayPort
{
    private readonly HttpClient _httpClient;
    private readonly LiqPayOptions _options;

    public LiqPayClient(HttpClient httpClient, IOptions<LiqPayOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.PaymentLatencyMs, new KeyValuePair<string, object?>("operation", "create_payment"));
        var payload = new
        {
            public_key = _options.PublicKey,
            version = "3",
            action = "pay",
            amount = request.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            currency = request.Currency,
            description = request.Description,
            order_id = request.OrderNumber,
            server_url = BuildUrl(_options.CallbackBaseUrl, request.CallbackUrl),
            result_url = BuildUrl(_options.ResultBaseUrl, request.ResultUrl)
        };

        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var signature = BuildSignature(data);
        var response = await SendFormAsync("request", data, signature, ct);

        if (!response.IsSuccess)
        {
            MarketplaceMetrics.PaymentErrors.Add(1, [new KeyValuePair<string, object?>("operation", "create_payment")]);
            return new LiqPayCreatePaymentResult(false, null, null, data, signature, response.RawResponse, response.Error);
        }
        MarketplaceMetrics.PaymentOps.Add(1, [new KeyValuePair<string, object?>("operation", "create_payment"), new KeyValuePair<string, object?>("status", "success")]);

        var json = response.Json!.Value;
        var transactionId = TryGetString(json, "liqpay_order_id") ?? TryGetString(json, "order_id");
        var checkoutUrl = $"{_options.CheckoutBaseUrl}?data={Uri.EscapeDataString(data)}&signature={Uri.EscapeDataString(signature)}";
        return new LiqPayCreatePaymentResult(true, transactionId, checkoutUrl, data, signature, response.RawResponse, null);
    }

    public Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default)
    {
        var expected = BuildSignature(data);
        return Task.FromResult(string.Equals(expected, signature, StringComparison.Ordinal));
    }

    public async Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.PaymentLatencyMs, new KeyValuePair<string, object?>("operation", "status"));
        var payload = new
        {
            public_key = _options.PublicKey,
            version = "3",
            action = "status",
            order_id = transactionId
        };
        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var signature = BuildSignature(data);
        var response = await SendFormAsync("request", data, signature, ct);
        if (!response.IsSuccess)
        {
            MarketplaceMetrics.PaymentErrors.Add(1, [new KeyValuePair<string, object?>("operation", "status")]);
            return new LiqPayPaymentStatusResult(false, transactionId, null, response.RawResponse, response.Error);
        }
        MarketplaceMetrics.PaymentOps.Add(1, [new KeyValuePair<string, object?>("operation", "status"), new KeyValuePair<string, object?>("status", "success")]);

        var json = response.Json!.Value;
        var status = TryGetString(json, "status");
        var providerTx = TryGetString(json, "liqpay_order_id") ?? transactionId;
        return new LiqPayPaymentStatusResult(true, providerTx, status, response.RawResponse, null);
    }

    public async Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.PaymentLatencyMs, new KeyValuePair<string, object?>("operation", "refund"));
        var payload = new
        {
            public_key = _options.PublicKey,
            version = "3",
            action = "refund",
            order_id = request.TransactionId,
            amount = request.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            currency = request.Currency,
            description = request.Description
        };
        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var signature = BuildSignature(data);
        var response = await SendFormAsync("request", data, signature, ct);
        if (!response.IsSuccess)
        {
            MarketplaceMetrics.PaymentErrors.Add(1, [new KeyValuePair<string, object?>("operation", "refund")]);
            return new LiqPayRefundResult(false, request.TransactionId, null, response.RawResponse, response.Error);
        }
        MarketplaceMetrics.PaymentOps.Add(1, [new KeyValuePair<string, object?>("operation", "refund"), new KeyValuePair<string, object?>("status", "success")]);

        var json = response.Json!.Value;
        var status = TryGetString(json, "status");
        var tx = TryGetString(json, "liqpay_order_id") ?? request.TransactionId;
        return new LiqPayRefundResult(true, tx, status, response.RawResponse, null);
    }

    public async Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default)
    {
        var config = CheckConfig();
        if (!config.IsHealthy)
            return new LiqPayHealthResult(false, "LiqPay", config.Message);

        var status = await GetPaymentStatusAsync("health-probe", ct);
        if (status.IsSuccess || (status.Error?.Contains("order", StringComparison.OrdinalIgnoreCase) ?? false))
            return new LiqPayHealthResult(true, "LiqPay", "LiqPay API is reachable.", 200);

        return new LiqPayHealthResult(false, "LiqPay", status.Error ?? "LiqPay readiness failed.", 503);
    }

    public LiqPayConfigHealthResult CheckConfig()
    {
        if (string.IsNullOrWhiteSpace(_options.PublicKey) || string.IsNullOrWhiteSpace(_options.PrivateKey))
            return new LiqPayConfigHealthResult(false, "LiqPay keys are not configured.");
        return new LiqPayConfigHealthResult(true, "LiqPay keys are configured.");
    }

    private async Task<(bool IsSuccess, JsonElement? Json, string? RawResponse, string? Error)> SendFormAsync(
        string endpoint,
        string data,
        string signature,
        CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiUrl.TrimEnd('/')}/3/{endpoint}")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["data"] = data,
                    ["signature"] = signature
                })
            };

            var response = await _httpClient.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, raw, $"HTTP {(int)response.StatusCode}: {raw}");

            using var doc = JsonDocument.Parse(raw);
            return (true, doc.RootElement.Clone(), raw, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
    }

    private string BuildSignature(string data)
    {
        var input = $"{_options.PrivateKey}{data}{_options.PrivateKey}";
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }

    private static string BuildUrl(string baseUrl, string path)
    {
        var left = baseUrl.TrimEnd('/');
        var right = path.StartsWith('/') ? path : "/" + path;
        return left + right;
    }

    private static string? TryGetString(JsonElement json, string name)
    {
        return json.TryGetProperty(name, out var value) ? value.GetString() : null;
    }
}
