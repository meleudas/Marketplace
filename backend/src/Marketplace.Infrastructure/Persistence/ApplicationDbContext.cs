using Marketplace.Application.Common.Interfaces;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MarketplaceUserRecord> MarketplaceUsers => Set<MarketplaceUserRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
    public DbSet<CompanyRecord> Companies => Set<CompanyRecord>();
    public DbSet<CompanyMemberRecord> CompanyMembers => Set<CompanyMemberRecord>();
    public DbSet<CompanyLegalProfileRecord> CompanyLegalProfiles => Set<CompanyLegalProfileRecord>();
    public DbSet<CompanyContractRecord> CompanyContracts => Set<CompanyContractRecord>();
    public DbSet<CompanyCommissionRateRecord> CompanyCommissionRates => Set<CompanyCommissionRateRecord>();
    public DbSet<CategoryRecord> Categories => Set<CategoryRecord>();
    public DbSet<WarehouseRecord> Warehouses => Set<WarehouseRecord>();
    public DbSet<WarehouseStockRecord> WarehouseStocks => Set<WarehouseStockRecord>();
    public DbSet<StockMovementRecord> StockMovements => Set<StockMovementRecord>();
    public DbSet<InventoryReservationRecord> InventoryReservations => Set<InventoryReservationRecord>();
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<ProductDetailRecord> ProductDetails => Set<ProductDetailRecord>();
    public DbSet<ProductImageRecord> ProductImages => Set<ProductImageRecord>();
    public DbSet<CartRecord> Carts => Set<CartRecord>();
    public DbSet<CartItemRecord> CartItems => Set<CartItemRecord>();
    public DbSet<FavoriteRecord> Favorites => Set<FavoriteRecord>();
    public DbSet<OrderRecord> Orders => Set<OrderRecord>();
    public DbSet<OrderItemRecord> OrderItems => Set<OrderItemRecord>();
    public DbSet<OrderAddressSnapshotRecord> OrderAddresses => Set<OrderAddressSnapshotRecord>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<RefundRecord> Refunds => Set<RefundRecord>();
    public DbSet<ProductReviewRecord> ProductReviews => Set<ProductReviewRecord>();
    public DbSet<CompanyReviewRecord> CompanyReviews => Set<CompanyReviewRecord>();
    public DbSet<ReviewReplyRecord> ReviewReplies => Set<ReviewReplyRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
