namespace Marketplace.Application.Finance.Ports;

public sealed record SellerPayoutRequest(
    Guid CompanyId,
    long SettlementBatchId,
    decimal Amount,
    string Currency,
    string? Iban,
    string? RecipientName,
    string? ProviderAccountId);

public sealed record SellerPayoutResult(
    bool IsSuccess,
    string? ProviderReference,
    string? Error);

public interface ISellerPayoutPort
{
    Task<SellerPayoutResult> InitiatePayoutAsync(SellerPayoutRequest request, CancellationToken ct = default);
}
