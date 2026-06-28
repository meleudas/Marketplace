using Marketplace.Application.Finance.Ports;
using Microsoft.Extensions.Logging;

namespace Marketplace.Infrastructure.External.Finance;

public sealed class ManualSellerPayoutAdapter : ISellerPayoutPort
{
    private readonly ILogger<ManualSellerPayoutAdapter> _logger;

    public ManualSellerPayoutAdapter(ILogger<ManualSellerPayoutAdapter> logger) => _logger = logger;

    public Task<SellerPayoutResult> InitiatePayoutAsync(SellerPayoutRequest request, CancellationToken ct = default)
    {
        var reference = $"manual-{request.SettlementBatchId}-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Manual payout queued for company {CompanyId}, batch {BatchId}, amount {Amount} {Currency}, IBAN {Iban}",
            request.CompanyId,
            request.SettlementBatchId,
            request.Amount,
            request.Currency,
            request.Iban);

        return Task.FromResult(new SellerPayoutResult(true, reference, null));
    }
}
