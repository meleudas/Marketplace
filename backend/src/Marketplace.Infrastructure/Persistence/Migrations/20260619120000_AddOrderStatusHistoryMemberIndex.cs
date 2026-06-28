using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Marketplace.Infrastructure.Persistence;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260619120000_AddOrderStatusHistoryMemberIndex")]
public partial class AddOrderStatusHistoryMemberIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_order_status_history_ChangedByUserId_OrderId",
            table: "order_status_history",
            columns: new[] { "ChangedByUserId", "OrderId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_order_status_history_ChangedByUserId_OrderId",
            table: "order_status_history");
    }
}
