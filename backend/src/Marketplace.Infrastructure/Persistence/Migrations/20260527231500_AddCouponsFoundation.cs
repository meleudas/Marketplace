using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    public partial class AddCouponsFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    DiscountType = table.Column<short>(type: "smallint", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    UserUsageLimit = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApplicableCategoriesRaw = table.Column<string>(type: "jsonb", nullable: true),
                    ApplicableProductsRaw = table.Column<string>(type: "jsonb", nullable: true),
                    ApplicableCompaniesRaw = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_coupons", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "coupon_usages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CouponId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CouponCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DiscountAppliedAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_coupon_usages", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "cart_coupon_links",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<long>(type: "bigint", nullable: false),
                    CouponId = table.Column<long>(type: "bigint", nullable: false),
                    CouponCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidationSnapshotRaw = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_cart_coupon_links", x => x.Id); });

            migrationBuilder.CreateIndex(
                name: "IX_coupons_Code",
                table: "coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupons_IsActive_StartsAtUtc_ExpiresAtUtc",
                table: "coupons",
                columns: new[] { "IsActive", "StartsAtUtc", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_usages_CouponId_OrderId",
                table: "coupon_usages",
                columns: new[] { "CouponId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupon_usages_CouponId_UserId",
                table: "coupon_usages",
                columns: new[] { "CouponId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_cart_coupon_links_CartId",
                table: "cart_coupon_links",
                column: "CartId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cart_coupon_links_CouponId",
                table: "cart_coupon_links",
                column: "CouponId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cart_coupon_links");
            migrationBuilder.DropTable(name: "coupon_usages");
            migrationBuilder.DropTable(name: "coupons");
        }
    }
}
