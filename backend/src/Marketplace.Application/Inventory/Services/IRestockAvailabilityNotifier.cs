namespace Marketplace.Application.Inventory.Services;

/// <summary>Notifies users watching a product when aggregate available stock crosses from zero to in-stock.</summary>
public interface IRestockAvailabilityNotifier
{
    Task NotifyIfCrossedFromZeroAsync(
        Guid companyId,
        long productId,
        int beforeAvailableSum,
        int afterAvailableSum,
        CancellationToken ct = default);
}
