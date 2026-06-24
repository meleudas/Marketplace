namespace Marketplace.Application.Finance.DTOs;

public sealed record SellerPayoutDto(
    long Id,
    long SettlementBatchId,
    string Status,
    decimal Amount,
    string Currency,
    string? ProviderReference,
    string? FailureReason,
    DateTime? CompletedAtUtc);

public sealed record SettlementBatchDto(
    long Id,
    Guid CompanyId,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime? ClosedAtUtc,
    DateTime? PaidAtUtc,
    SellerPayoutDto? Payout);

public sealed record CompanyPayoutProfileDto(
    Guid CompanyId,
    string? PayoutIban,
    string? PayoutRecipientName,
    string? PayoutProviderAccountId);

public sealed record CompanyCommissionRateHistoryDto(
    long Id,
    decimal CommissionPercent,
    DateTime EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    string? Reason);
