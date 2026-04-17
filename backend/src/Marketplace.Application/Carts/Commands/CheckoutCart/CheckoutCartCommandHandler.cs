using Marketplace.Application.Carts.Cache;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Common.Ports;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Carts.Commands.CheckoutCart;

public sealed class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Result<CheckoutResultDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IOrderAddressSnapshotRepository _orderAddressRepository;
    private readonly IAppCachePort _cache;

    public CheckoutCartCommandHandler(
        ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        IOrderAddressSnapshotRepository orderAddressRepository,
        IAppCachePort cache)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _orderAddressRepository = orderAddressRepository;
        _cache = cache;
    }

    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCartCommand request, CancellationToken ct)
    {
        try
        {
            var cart = await _cartRepository.GetActiveByUserIdAsync(request.ActorUserId, ct);
            if (cart is null)
                return Result<CheckoutResultDto>.Failure("Cart not found");

            var cartItems = await _cartItemRepository.ListByCartIdAsync(cart.Id, ct);
            if (cartItems.Count == 0)
                return Result<CheckoutResultDto>.Failure("Cart is empty");

            var productIds = cartItems.Select(x => x.ProductId).Distinct().ToArray();
            var products = await _productRepository.ListByIdsAsync(productIds, ct);
            var productMap = products.ToDictionary(x => x.Id.Value, x => x);

            foreach (var item in cartItems)
            {
                if (!productMap.ContainsKey(item.ProductId.Value))
                    return Result<CheckoutResultDto>.Failure($"Product '{item.ProductId.Value}' not found");
            }

            var now = DateTime.UtcNow;
            var createdOrders = new List<CreatedOrderDto>();

            foreach (var companyGroup in cartItems.GroupBy(x => productMap[x.ProductId.Value].CompanyId.Value))
            {
                var orderNumber = $"ORD-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
                var subtotal = companyGroup.Sum(x => x.PriceAtMoment.Amount * x.Quantity);

                var order = Order.Reconstitute(
                    OrderId.From(0),
                    orderNumber,
                    request.ActorUserId,
                    CompanyId.From(companyGroup.Key),
                    OrderStatus.Pending,
                    new Money(subtotal),
                    new Money(subtotal),
                    Money.Zero,
                    Money.Zero,
                    Money.Zero,
                    ShippingMethodId.From(0),
                    request.PaymentMethod,
                    request.Notes,
                    null,
                    null,
                    null,
                    null,
                    null,
                    now,
                    now,
                    false,
                    null);

                var savedOrder = await _orderRepository.AddAsync(order, ct);

                var orderItems = companyGroup
                    .Select(item =>
                    {
                        var product = productMap[item.ProductId.Value];
                        var lineTotal = item.PriceAtMoment.Amount * item.Quantity;
                        return OrderItem.Reconstitute(
                            OrderItemId.From(0),
                            savedOrder.Id,
                            item.ProductId,
                            product.Name,
                            null,
                            item.Quantity,
                            item.PriceAtMoment,
                            item.Discount,
                            new Money(lineTotal),
                            product.CompanyId,
                            now,
                            now,
                            false,
                            null);
                    })
                    .ToList();

                await _orderItemRepository.AddRangeAsync(orderItems, ct);

                var address = OrderAddressSnapshot.Reconstitute(
                    OrderAddressId.From(0),
                    savedOrder.Id,
                    OrderAddressKind.Shipping,
                    ContactPerson.Create(request.Address.FirstName, request.Address.LastName, request.Address.Phone),
                    Address.Create(
                        request.Address.Street,
                        request.Address.City,
                        request.Address.State,
                        request.Address.PostalCode,
                        request.Address.Country),
                    now,
                    now,
                    false,
                    null);

                await _orderAddressRepository.AddRangeAsync([address], ct);

                createdOrders.Add(new CreatedOrderDto(
                    savedOrder.Id.Value,
                    savedOrder.OrderNumber,
                    savedOrder.CompanyId.Value,
                    savedOrder.Status,
                    orderItems.Count,
                    savedOrder.TotalPrice.Amount));
            }

            await _cartItemRepository.SoftDeleteByCartIdAsync(cart.Id, now, ct);
            await _cache.RemoveAsync(CartCacheKeys.ActiveByUser(request.ActorUserId), ct);

            return Result<CheckoutResultDto>.Success(new CheckoutResultDto(createdOrders));
        }
        catch (Exception ex)
        {
            return Result<CheckoutResultDto>.Failure($"Failed to checkout cart: {ex.Message}");
        }
    }
}
