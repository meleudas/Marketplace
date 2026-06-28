namespace Marketplace.Domain.Finance.Enums;

public enum SellerLedgerEntryType : short
{
    Sale = 1,
    PlatformFee = 2,
    Refund = 3,
    Payout = 4,
    Adjustment = 5
}
