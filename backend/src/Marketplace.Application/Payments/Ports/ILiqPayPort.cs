namespace Marketplace.Application.Payments.Ports;

public interface ILiqPayPort
{
    Task<LiqPayCreatePaymentResult> CreatePaymentAsync(LiqPayCreatePaymentRequest request, CancellationToken ct = default);
    Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken ct = default);
    Task<LiqPayPaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default);
    Task<LiqPayRefundResult> RefundAsync(LiqPayRefundRequest request, CancellationToken ct = default);
    Task<LiqPayHealthResult> CheckReadinessAsync(CancellationToken ct = default);
    LiqPayConfigHealthResult CheckConfig();
}

public sealed record LiqPayCreatePaymentRequest(
    string OrderNumber,
    decimal Amount,
    string Currency,
    string Description,
    string CallbackUrl,
    string ResultUrl);

public sealed record LiqPayCreatePaymentResult(
    bool IsSuccess,
    string? TransactionId,
    string? CheckoutUrl,
    string Data,
    string Signature,
    string? RawResponse,
    string? Error);

public sealed record LiqPayPaymentStatusResult(
    bool IsSuccess,
    string? TransactionId,
    string? Status,
    string? RawResponse,
    string? Error);

public sealed record LiqPayRefundRequest(string TransactionId, decimal Amount, string Currency, string? Description);
public sealed record LiqPayRefundResult(bool IsSuccess, string? TransactionId, string? Status, string? RawResponse, string? Error);
public sealed record LiqPayHealthResult(bool IsHealthy, string Provider, string Message, int? StatusCode = null);
public sealed record LiqPayConfigHealthResult(bool IsHealthy, string Message);
