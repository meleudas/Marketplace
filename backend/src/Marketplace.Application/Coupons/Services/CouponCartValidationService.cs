using Marketplace.Application.Coupons.DTOs;
using Marketplace.Domain.Cart.Repositories;
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
    private readonly ICouponUsageRepository _couponUsageRepository;

    public CouponCartValidationService(
        ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        ICouponRepository couponRepository,
        ICouponUsageRepository couponUsageRepository)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
        _couponUsageRepository = couponUsageRepository;
    }

    public async Task<(CouponValidationResultDto Result, Coupon? Coupon, CartId? CartId)> ValidateAsync(Guid actorUserId, string code, CancellationToken ct)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(actorUserId, ct);
        if (cart is null)
            return (new CouponValidationResultDto(false, "not_found", "Cart not found", null, 0, 0, 0), null, null);

        var items = await _cartItemRepository.ListByCartIdAsync(cart.Id, ct);
        if (items.Count == 0)
            return (new CouponValidationResultDto(false, "unprocessable", "Cart is empty", null, 0, 0, 0), null, cart.Id);

        var subtotal = items.Sum(x => x.PriceAtMoment.Amount * x.Quantity);
        var coupon = await _couponRepository.GetByCodeAsync(code.Trim(), ct);
        if (coupon is null)
            return (new CouponValidationResultDto(false, "not_found", "Coupon not found", code.Trim(), subtotal, 0, subtotal), null, cart.Id);

        if (!coupon.IsValidAt(DateTime.UtcNow))
            return (new CouponValidationResultDto(false, "unprocessable", "Coupon is not active", coupon.Code, subtotal, 0, subtotal), null, cart.Id);

        var productIds = items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await _productRepository.ListByIdsAsync(productIds, ct);
        var hasInScopeCompany = items.Any(item =>
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            return product is not null && coupon.IsCompanyInScope(product.CompanyId.Value);
        });
        if (!hasInScopeCompany)
            return (new CouponValidationResultDto(false, "forbidden", "Coupon does not apply to cart companies", coupon.Code, subtotal, 0, subtotal), null, cart.Id);

        var userUsageCount = await _couponUsageRepository.CountByCouponAndUserAsync(coupon.Id, actorUserId, ct);
        if (!coupon.IsUsageAvailableFor(actorUserId, userUsageCount))
            return (new CouponValidationResultDto(false, "conflict", "Coupon usage limit reached", coupon.Code, subtotal, 0, subtotal), null, cart.Id);

        if (!coupon.IsEligibleForSubtotal(new Money(subtotal)))
            return (new CouponValidationResultDto(false, "unprocessable", "Cart does not satisfy min order amount", coupon.Code, subtotal, 0, subtotal), null, cart.Id);

        var discount = coupon.CalculateDiscount(new Money(subtotal)).Amount;
        return (new CouponValidationResultDto(true, null, null, coupon.Code, subtotal, discount, subtotal - discount), coupon, cart.Id);
    }
}
