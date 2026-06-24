using Marketplace.Application.Coupons.DTOs;
using Marketplace.Application.Coupons.Validation;
using Marketplace.Domain.Cart.Entities;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Catalog.Entities;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Coupons.Entities;
using Marketplace.Domain.Coupons.Repositories;

namespace Marketplace.Application.Coupons.Services;

public sealed class CouponCartValidationService
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly CouponEligibilityEvaluator _eligibility;

    public CouponCartValidationService(
        ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        ICouponRepository couponRepository,
        CouponEligibilityEvaluator eligibility)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
        _eligibility = eligibility;
    }

    public async Task<(CouponValidationResultDto Result, Coupon? Coupon, CartId? CartId)> ValidateAsync(Guid actorUserId, string code, CancellationToken ct)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(actorUserId, ct);
        if (cart is null)
            return (new CouponValidationResultDto(false, "not_found", "Cart not found", null, 0, 0, 0), null, null);

        var items = await _cartItemRepository.ListByCartIdAsync(cart.Id, ct);
        if (items.Count == 0)
            return (new CouponValidationResultDto(false, "unprocessable", "Cart is empty", null, 0, 0, 0), null, cart.Id);

        var coupon = await _couponRepository.GetByCodeAsync(code.Trim(), ct);
        if (coupon is null)
        {
            var subtotal = items.Sum(x => x.PriceAtMoment.Amount * x.Quantity);
            return (new CouponValidationResultDto(false, "not_found", "Coupon not found", code.Trim(), subtotal, 0, subtotal), null, cart.Id);
        }

        var lines = await BuildLinesAsync(items, ct);
        var eligibility = await _eligibility.EvaluateAsync(coupon, actorUserId, lines, ct);
        if (!eligibility.IsValid)
        {
            return (new CouponValidationResultDto(
                false,
                eligibility.ErrorCode,
                eligibility.Message,
                coupon.Code,
                eligibility.CartSubtotal,
                0,
                eligibility.CartSubtotal), null, cart.Id);
        }

        return (new CouponValidationResultDto(
            true,
            null,
            null,
            coupon.Code,
            eligibility.CartSubtotal,
            eligibility.DiscountAmount,
            eligibility.CartSubtotal - eligibility.DiscountAmount), coupon, cart.Id);
    }

    public async Task<CouponEligibilityResult> ValidateCartAsync(
        Guid actorUserId,
        CartId cartId,
        IReadOnlyList<CartItem> items,
        IReadOnlyDictionary<long, Product> productMap,
        Coupon coupon,
        CancellationToken ct)
    {
        var lines = items
            .Where(x => productMap.ContainsKey(x.ProductId.Value))
            .Select(x =>
            {
                var product = productMap[x.ProductId.Value];
                return new CouponCartLine(
                    x.ProductId.Value,
                    product.CategoryId.Value,
                    product.CompanyId.Value,
                    x.Quantity,
                    x.PriceAtMoment.Amount);
            })
            .ToList();

        return await _eligibility.EvaluateAsync(coupon, actorUserId, lines, ct);
    }

    internal async Task<IReadOnlyList<CouponCartLine>> BuildLinesForCartAsync(CartId cartId, CancellationToken ct)
    {
        var items = await _cartItemRepository.ListByCartIdAsync(cartId, ct);
        return await BuildLinesAsync(items, ct);
    }

    private async Task<IReadOnlyList<CouponCartLine>> BuildLinesAsync(IReadOnlyList<CartItem> items, CancellationToken ct)
    {
        var productIds = items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await _productRepository.ListByIdsAsync(productIds, ct);
        var productMap = products.ToDictionary(x => x.Id.Value, x => x);

        return items
            .Where(x => productMap.ContainsKey(x.ProductId.Value))
            .Select(x =>
            {
                var product = productMap[x.ProductId.Value];
                return new CouponCartLine(
                    x.ProductId.Value,
                    product.CategoryId.Value,
                    product.CompanyId.Value,
                    x.Quantity,
                    x.PriceAtMoment.Amount);
            })
            .ToList();
    }
}
