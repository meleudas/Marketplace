using Marketplace.Application.Carts.Cache;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Carts.DTOs;
using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Common;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Coupons.Services;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Ports;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Orders.Entities;
using Marketplace.Domain.Orders.Enums;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Entities;
using Marketplace.Domain.Payments.Enums;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Marketplace.Application.Carts.Commands.CheckoutCart;

public sealed class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Result<CheckoutResultDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IOrderAddressSnapshotRepository _orderAddressRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILiqPayPort _liqPayPort;
    private readonly IAppCachePort _cache;
    private readonly OrderMutationCoordinator _orderMutationCoordinator;
    private readonly ICheckoutInventoryService _checkoutInventory;
    private readonly IOrderStatusHistoryWriter _historyWriter;
        private readonly IWarehouseStockRepository _warehouseStockRepository;
        private readonly WarehouseAllocationPlanner _warehouseAllocationPlanner;
    private readonly IAppNotificationScheduler _appNotifications;
    private readonly ICartStockWatchRepository _cartStockWatches;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly ICouponCheckoutService _couponCheckoutService;
    private readonly IAppTransactionPort _tx;
    private readonly ILogger<CheckoutCartCommandHandler> _logger;

    public CheckoutCartCommandHandler(
        ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        IOrderAddressSnapshotRepository orderAddressRepository,
        IPaymentRepository paymentRepository,
        ILiqPayPort liqPayPort,
        IAppCachePort cache,
        OrderMutationCoordinator orderMutationCoordinator,
        ICheckoutInventoryService checkoutInventory,
        IOrderStatusHistoryWriter historyWriter,
        IWarehouseStockRepository warehouseStockRepository,
        WarehouseAllocationPlanner warehouseAllocationPlanner,
        IAppNotificationScheduler appNotifications,
        ICartStockWatchRepository cartStockWatches,
        IShippingMethodRepository shippingMethodRepository,
        ICouponCheckoutService couponCheckoutService,
        IAppTransactionPort tx,
        ILogger<CheckoutCartCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _orderAddressRepository = orderAddressRepository;
        _paymentRepository = paymentRepository;
        _liqPayPort = liqPayPort;
        _cache = cache;
        _orderMutationCoordinator = orderMutationCoordinator;
        _checkoutInventory = checkoutInventory;
        _historyWriter = historyWriter;
        _warehouseStockRepository = warehouseStockRepository;
        _warehouseAllocationPlanner = warehouseAllocationPlanner;
        _appNotifications = appNotifications;
        _cartStockWatches = cartStockWatches;
        _shippingMethodRepository = shippingMethodRepository;
        _couponCheckoutService = couponCheckoutService;
        _tx = tx;
        _logger = logger;
    }

    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCartCommand request, CancellationToken ct)
    {
        using var activity = MarketplaceTelemetry.StartActivity("checkout.execute");
        activity?.SetTag("operation", "checkout");

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

            var shippingMethod = await _shippingMethodRepository.GetByIdAsync(ShippingMethodId.From(request.ShippingMethodId), ct);
            if (shippingMethod is null || !shippingMethod.IsActive)
                return Result<CheckoutResultDto>.Failure("Shipping method not found");

            foreach (var grouped in cartItems.GroupBy(x => (companyId: productMap[x.ProductId.Value].CompanyId, x.ProductId)))
            {
                var companyId = grouped.Key.companyId;
                var productId = grouped.Key.ProductId;
                var requested = grouped.Sum(x => x.Quantity);
                var plan = await _warehouseAllocationPlanner.PlanAsync(
                    companyId,
                    [new WarehouseAllocationLineRequest(productId, requested)],
                    ct);
                if (!plan.IsValid)
                    return Result<CheckoutResultDto>.Failure("Insufficient stock for checkout");
            }

            var revalidation = await _couponCheckoutService.RevalidateForCheckoutAsync(
                request.ActorUserId,
                cart.Id,
                cartItems,
                productMap,
                ct);
            if (!revalidation.IsValid)
                return Result<CheckoutResultDto>.Failure($"{revalidation.ErrorCode}: {revalidation.Message}");

            var couponLines = cartItems
                .Select(item =>
                {
                    var product = productMap[item.ProductId.Value];
                    return new Marketplace.Application.Coupons.Validation.CouponCartLine(
                        item.ProductId.Value,
                        product.CategoryId.Value,
                        product.CompanyId.Value,
                        item.Quantity,
                        item.PriceAtMoment.Amount);
                })
                .ToList();
            var checkoutCouponPlan = await _couponCheckoutService.ResolveCheckoutPlanAsync(
                request.ActorUserId,
                cart.Id,
                couponLines,
                ct);
            if (!checkoutCouponPlan.IsValid)
                return Result<CheckoutResultDto>.Failure($"{checkoutCouponPlan.ErrorCode}: {checkoutCouponPlan.Message}");

            var now = DateTime.UtcNow;
            var createdOrders = new List<CreatedOrderDto>();
            var ordersToReleaseStock = new List<(long OrderId, Guid CompanyId)>();
            OrderId? primaryOrderId = null;

            await _tx.ExecuteAsync(async txCt =>
            {
                foreach (var companyGroup in cartItems.GroupBy(x => productMap[x.ProductId.Value].CompanyId.Value))
                {
                    var orderNumber = $"ORD-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
                    var subtotal = companyGroup.Sum(x => x.PriceAtMoment.Amount * x.Quantity);
                    var shippingCost = shippingMethod.Price.Amount;
                    var couponDiscountAmount = checkoutCouponPlan.Plan.GetDiscountForCompany(companyGroup.Key);
                    var total = subtotal + shippingCost - couponDiscountAmount;
                    if (total < 0)
                        total = 0;

                    var order = Order.Reconstitute(
                        OrderId.From(0),
                        orderNumber,
                        request.ActorUserId,
                        CompanyId.From(companyGroup.Key),
                        OrderStatus.Pending,
                        new Money(total),
                        new Money(subtotal),
                        new Money(shippingCost),
                        new Money(couponDiscountAmount),
                        Money.Zero,
                        shippingMethod.Id,
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

                    var savedOrder = await _orderRepository.AddAsync(order, txCt);
                    primaryOrderId ??= savedOrder.Id;

                    await _historyWriter.RecordCreatedAsync(
                        savedOrder,
                        request.ActorUserId,
                        "checkout",
                        correlationId: savedOrder.OrderNumber,
                        txCt);

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

                    await _orderItemRepository.AddRangeAsync(orderItems, txCt);

                    var savedItems = await _orderItemRepository.ListByOrderIdAsync(savedOrder.Id, txCt);
                    var reserveLines = savedItems
                        .Select(item => (item.Id, item.ProductId, item.Quantity))
                        .ToList();
                    await _checkoutInventory.ReserveForOrderAsync(
                        savedOrder.Id,
                        savedOrder.CompanyId,
                        reserveLines,
                        txCt);

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

                    await _orderAddressRepository.AddRangeAsync([address], txCt);

                    PaymentInitDto? paymentDto = null;
                    var paymentMethodKind = request.PaymentMethod switch
                    {
                        CheckoutPaymentMethod.Card => PaymentMethodKind.LiqPay,
                        CheckoutPaymentMethod.PayPal => PaymentMethodKind.PayPal,
                        CheckoutPaymentMethod.BankTransfer => PaymentMethodKind.BankTransfer,
                        CheckoutPaymentMethod.Cash => PaymentMethodKind.Cash,
                        _ => PaymentMethodKind.Card
                    };

                    if (paymentMethodKind == PaymentMethodKind.Cash)
                    {
                        var cashPayment = Payment.Create(
                            PaymentId.From(0),
                            savedOrder.Id,
                            PaymentMethodKind.Cash,
                            savedOrder.TotalPrice,
                            "UAH",
                            null,
                            PaymentTransactionStatus.Pending,
                            JsonBlob.Empty);
                        await _paymentRepository.AddAsync(cashPayment, txCt);
                    }
                    else
                    {
                        var callbackUrl = "/integrations/liqpay/webhook";
                        var resultUrl = $"/checkout/result?order_id={savedOrder.OrderNumber}";
                        var liqPayResult = await _liqPayPort.CreatePaymentAsync(
                            new LiqPayCreatePaymentRequest(
                                savedOrder.OrderNumber,
                                savedOrder.TotalPrice.Amount,
                                "UAH",
                                $"Marketplace order {savedOrder.OrderNumber}",
                                callbackUrl,
                                resultUrl),
                            txCt);

                        var payment = Payment.Create(
                            PaymentId.From(0),
                            savedOrder.Id,
                            paymentMethodKind == PaymentMethodKind.LiqPay ? PaymentMethodKind.LiqPay : paymentMethodKind,
                            savedOrder.TotalPrice,
                            "UAH",
                            liqPayResult.TransactionId,
                            liqPayResult.IsSuccess ? PaymentTransactionStatus.Pending : PaymentTransactionStatus.Failed,
                            new JsonBlob(liqPayResult.RawResponse));
                        await _paymentRepository.AddAsync(payment, txCt);

                        paymentDto = new PaymentInitDto(
                            "liqpay",
                            liqPayResult.IsSuccess ? "pending" : "pending_failed_init",
                            liqPayResult.TransactionId,
                            liqPayResult.Data,
                            liqPayResult.Signature,
                            liqPayResult.CheckoutUrl);

                        if (!liqPayResult.IsSuccess)
                        {
                            var oldStatus = savedOrder.Status;
                            savedOrder.MarkFailed();
                            await _orderRepository.UpdateAsync(savedOrder, txCt);
                            await _historyWriter.WriteIfChangedAsync(
                                savedOrder,
                                oldStatus,
                                request.ActorUserId,
                                "checkout",
                                correlationId: liqPayResult.TransactionId,
                                ct: txCt);
                            ordersToReleaseStock.Add((savedOrder.Id.Value, savedOrder.CompanyId.Value));
                        }
                    }

                    createdOrders.Add(new CreatedOrderDto(
                        savedOrder.Id.Value,
                        savedOrder.OrderNumber,
                        savedOrder.CompanyId.Value,
                        savedOrder.Status,
                        orderItems.Count,
                        savedOrder.TotalPrice.Amount,
                        paymentDto));

                    await _orderMutationCoordinator.PublishOrderChangedAsync(
                        savedOrder,
                        "OrderCreated",
                        $"checkout:{savedOrder.OrderNumber}",
                        txCt);

                    await _appNotifications.ScheduleAsync(
                        new AppNotificationRequest
                        {
                            TemplateKey = AppNotificationTemplateKeys.AdminNewOrder,
                            CorrelationId = Guid.NewGuid(),
                            Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
                            Audience = AppNotificationAudienceKind.Admins,
                            PayloadJson = JsonSerializer.Serialize(new
                            {
                                orderId = savedOrder.Id.Value,
                                orderNumber = savedOrder.OrderNumber,
                                companyId = savedOrder.CompanyId.Value
                            })
                        },
                        txCt);

                    var companyPayload = JsonSerializer.Serialize(new
                    {
                        orderId = savedOrder.Id.Value,
                        orderNumber = savedOrder.OrderNumber,
                        companyId = savedOrder.CompanyId.Value
                    });
                    await _appNotifications.ScheduleAsync(
                        new AppNotificationRequest
                        {
                            TemplateKey = AppNotificationTemplateKeys.CompanyNewOrder,
                            CorrelationId = AppNotificationCorrelationIds.Deterministic(
                                $"checkout-company|{savedOrder.Id.Value}|{savedOrder.CompanyId.Value}"),
                            Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
                            Audience = AppNotificationAudienceKind.CompanyStakeholders,
                            TargetCompanyId = savedOrder.CompanyId.Value,
                            PayloadJson = companyPayload
                        },
                        txCt);
                }

                if (primaryOrderId is not null
                    && checkoutCouponPlan is { CouponId: not null, CouponCode: not null }
                    && checkoutCouponPlan.Plan.TotalDiscount > 0)
                {
                    await _couponCheckoutService.ConsumeOnceAsync(
                        request.ActorUserId,
                        primaryOrderId,
                        cart.Id,
                        checkoutCouponPlan.CouponId.Value,
                        checkoutCouponPlan.CouponCode,
                        checkoutCouponPlan.Plan.TotalDiscount,
                        txCt);
                }

                await _cartItemRepository.SoftDeleteByCartIdAsync(cart.Id, now, txCt);
                await _cartStockWatches.DeleteAllForUserAsync(request.ActorUserId, txCt);
            }, ct);

            foreach (var (orderId, companyId) in ordersToReleaseStock)
            {
                await _checkoutInventory.ReleaseForOrderAsync(
                    OrderId.From(orderId),
                    CompanyId.From(companyId),
                    request.ActorUserId,
                    "checkout-payment-failed",
                    ct);
            }

            try
            {
                await _cache.RemoveAsync(CartCacheKeys.ActiveByUser(request.ActorUserId), ct);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Checkout completed but cart cache invalidation failed for user {ActorUserId}", request.ActorUserId);
            }

            return Result<CheckoutResultDto>.Success(new CheckoutResultDto(createdOrders));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient stock", StringComparison.OrdinalIgnoreCase))
        {
            return Result<CheckoutResultDto>.Failure("Insufficient stock for checkout");
        }
        catch (Exception ex)
        {
            return Result<CheckoutResultDto>.Failure($"Failed to checkout cart: {ex.Message}");
        }
    }
}
