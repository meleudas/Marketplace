using System;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260620130000_AddP4FinanceAndWarehouse")]
    public partial class AddP4FinanceAndWarehouse : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WarehouseId",
                table: "shipments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_WarehouseId",
                table: "shipments",
                column: "WarehouseId");

            migrationBuilder.AddColumn<string>(
                name: "PayoutIban",
                table: "company_legal_profiles",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutProviderAccountId",
                table: "company_legal_profiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutRecipientName",
                table: "company_legal_profiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_fulfillment_allocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservationId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_fulfillment_allocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillment_allocations_OrderId",
                table: "order_fulfillment_allocations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillment_allocations_OrderId_WarehouseId",
                table: "order_fulfillment_allocations",
                columns: new[] { "OrderId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillment_allocations_OrderItemId",
                table: "order_fulfillment_allocations",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillment_allocations_ReservationId",
                table: "order_fulfillment_allocations",
                column: "ReservationId");

            migrationBuilder.CreateTable(
                name: "order_financials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MerchandiseSubtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MerchandiseBase = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    PlatformFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellerMerchandiseNet = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellerPayoutEligible = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_financials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_financials_CompanyId",
                table: "order_financials",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_order_financials_OrderId",
                table: "order_financials",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_financials_PaymentId",
                table: "order_financials",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateTable(
                name: "seller_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderFinancialsId = table.Column<long>(type: "bigint", nullable: true),
                    SettlementBatchId = table.Column<long>(type: "bigint", nullable: true),
                    SellerPayoutId = table.Column<long>(type: "bigint", nullable: true),
                    EntryType = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AvailableAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_ledger_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seller_ledger_entries_CompanyId",
                table: "seller_ledger_entries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_seller_ledger_entries_CompanyId_Status",
                table: "seller_ledger_entries",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_seller_ledger_entries_OrderId",
                table: "seller_ledger_entries",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_seller_ledger_entries_OrderId_EntryType",
                table: "seller_ledger_entries",
                columns: new[] { "OrderId", "EntryType" });

            migrationBuilder.CreateIndex(
                name: "IX_seller_ledger_entries_SellerPayoutId",
                table: "seller_ledger_entries",
                column: "SellerPayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_seller_ledger_entries_SettlementBatchId",
                table: "seller_ledger_entries",
                column: "SettlementBatchId");

            migrationBuilder.CreateTable(
                name: "settlement_batches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement_batches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_batches_CompanyId_Status",
                table: "settlement_batches",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_batches_PeriodEndUtc",
                table: "settlement_batches",
                column: "PeriodEndUtc");

            migrationBuilder.CreateTable(
                name: "seller_payouts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementBatchId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    InitiatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_payouts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seller_payouts_CompanyId",
                table: "seller_payouts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_seller_payouts_SettlementBatchId",
                table: "seller_payouts",
                column: "SettlementBatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seller_payouts_Status",
                table: "seller_payouts",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "order_fulfillment_allocations");
            migrationBuilder.DropTable(name: "seller_ledger_entries");
            migrationBuilder.DropTable(name: "seller_payouts");
            migrationBuilder.DropTable(name: "settlement_batches");
            migrationBuilder.DropTable(name: "order_financials");

            migrationBuilder.DropIndex(name: "IX_shipments_WarehouseId", table: "shipments");
            migrationBuilder.DropColumn(name: "WarehouseId", table: "shipments");

            migrationBuilder.DropColumn(name: "PayoutIban", table: "company_legal_profiles");
            migrationBuilder.DropColumn(name: "PayoutProviderAccountId", table: "company_legal_profiles");
            migrationBuilder.DropColumn(name: "PayoutRecipientName", table: "company_legal_profiles");
        }
    }
}
