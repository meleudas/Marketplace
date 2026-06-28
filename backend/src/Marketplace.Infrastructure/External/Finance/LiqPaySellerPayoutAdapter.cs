using System.Net.Http.Json;
using Marketplace.Application.Finance.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Finance;

public sealed class LiqPaySellerPayoutAdapter : ISellerPayoutPort
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LiqPaySellerPayoutAdapter> _logger;

    public LiqPaySellerPayoutAdapter(HttpClient httpClient, ILogger<LiqPaySellerPayoutAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SellerPayoutResult> InitiatePayoutAsync(SellerPayoutRequest request, CancellationToken ct = default)
    {
        try
        {
            // Stub: LiqPay payout API is not wired in MVP — record intent and return synthetic reference.
            var payload = new
            {
                companyId = request.CompanyId,
                settlementBatchId = request.SettlementBatchId,
                amount = request.Amount,
                currency = request.Currency,
                iban = request.Iban,
                recipientName = request.RecipientName,
                providerAccountId = request.ProviderAccountId
            };

            using var response = await _httpClient.PostAsJsonAsync("https://www.liqpay.ua/api/payout/stub", payload, ct);
            var reference = $"liqpay-stub-{request.SettlementBatchId}";
            _logger.LogInformation(
                "LiqPay payout stub invoked for company {CompanyId}, HTTP {StatusCode}",
                request.CompanyId,
                (int)response.StatusCode);

            return new SellerPayoutResult(true, reference, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LiqPay payout stub failed for company {CompanyId}", request.CompanyId);
            return new SellerPayoutResult(false, null, ex.Message);
        }
    }
}
