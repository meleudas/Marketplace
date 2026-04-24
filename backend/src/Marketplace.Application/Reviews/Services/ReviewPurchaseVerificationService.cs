using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;

namespace Marketplace.Application.Reviews.Services;

public sealed class ReviewPurchaseVerificationService : IReviewPurchaseVerificationService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;

    public ReviewPurchaseVerificationService(IOrderRepository orderRepository, IOrderItemRepository orderItemRepository)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
    }

    public async Task<long?> GetVerifiedProductOrderIdAsync(Guid userId, ProductId productId, CancellationToken ct = default)
    {
        var orders = await _orderRepository.ListByCustomerAsync(userId, ct);
        var eligible = orders
            .Where(o => o.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered)
            .OrderByDescending(o => o.CreatedAt);

        foreach (var order in eligible)
        {
            var items = await _orderItemRepository.ListByOrderIdAsync(order.Id, ct);
            if (items.Any(i => i.ProductId == productId))
                return order.Id.Value;
        }

        return null;
    }

    public async Task<long?> GetVerifiedCompanyOrderIdAsync(Guid userId, CompanyId companyId, CancellationToken ct = default)
    {
        var orders = await _orderRepository.ListByCustomerAsync(userId, ct);
        var match = orders
            .Where(o => o.CompanyId == companyId)
            .Where(o => o.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();
        return match?.Id.Value;
    }
}
