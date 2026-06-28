namespace Marketplace.Domain.Finance.Enums;

public enum SettlementBatchStatus : short
{
    Open = 1,
    Ready = 2,
    Processing = 3,
    Paid = 4,
    Failed = 5
}
