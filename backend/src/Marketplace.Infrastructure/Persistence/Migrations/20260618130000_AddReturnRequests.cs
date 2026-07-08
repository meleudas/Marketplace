using System;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260618130000_AddReturnRequests")]
    public partial class AddReturnRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "return_requests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ReasonCode = table.Column<short>(type: "smallint", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_return_requests", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_return_requests_CompanyId", table: "return_requests", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_return_requests_CustomerId", table: "return_requests", column: "CustomerId");
            migrationBuilder.CreateIndex(name: "IX_return_requests_OrderId", table: "return_requests", column: "OrderId");
            migrationBuilder.CreateIndex(name: "IX_return_requests_Status", table: "return_requests", column: "Status");

            migrationBuilder.CreateTable(
                name: "return_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReturnRequestId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_return_items", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_return_items_OrderItemId", table: "return_items", column: "OrderItemId");
            migrationBuilder.CreateIndex(name: "IX_return_items_ReturnRequestId", table: "return_items", column: "ReturnRequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "return_items");
            migrationBuilder.DropTable(name: "return_requests");
        }
    }
}
