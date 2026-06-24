namespace Marketplace.Application.Finance.DTOs;

public sealed record SellerEarningsSummaryDto(
    Guid CompanyId,
    decimal PendingAmount,
    decimal AvailableAmount,
    decimal SettledAmount,
    decimal TotalPlatformFees,
    string Currency);
