using System;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260618120000_AddShipmentItemsAndFulfillment")]
    public partial class AddShipmentItemsAndFulfillment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipments_OrderId",
                table: "shipments");

            migrationBuilder.AddColumn<int>(
                name: "ShipmentNumber",
                table: "shipments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_OrderId",
                table: "shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_TrackingNumber",
                table: "shipments",
                column: "TrackingNumber");

            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "shipping_events",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShipmentId",
                table: "shipping_events",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "shipping_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "DeliveryStatus",
                table: "shipping_events",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAtUtc",
                table: "shipping_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_events_OrderId",
                table: "shipping_events",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_events_ShipmentId",
                table: "shipping_events",
                column: "ShipmentId");

            migrationBuilder.CreateTable(
                name: "shipment_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_items_OrderItemId",
                table: "shipment_items",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_items_ShipmentId_OrderItemId",
                table: "shipment_items",
                columns: new[] { "ShipmentId", "OrderItemId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "shipment_items");

            migrationBuilder.DropIndex(name: "IX_shipping_events_ShipmentId", table: "shipping_events");
            migrationBuilder.DropIndex(name: "IX_shipping_events_OrderId", table: "shipping_events");
            migrationBuilder.DropColumn(name: "OccurredAtUtc", table: "shipping_events");
            migrationBuilder.DropColumn(name: "DeliveryStatus", table: "shipping_events");
            migrationBuilder.DropColumn(name: "TrackingNumber", table: "shipping_events");
            migrationBuilder.DropColumn(name: "ShipmentId", table: "shipping_events");
            migrationBuilder.DropColumn(name: "OrderId", table: "shipping_events");

            migrationBuilder.DropIndex(name: "IX_shipments_TrackingNumber", table: "shipments");
            migrationBuilder.DropIndex(name: "IX_shipments_OrderId", table: "shipments");
            migrationBuilder.DropColumn(name: "ShipmentNumber", table: "shipments");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_OrderId",
                table: "shipments",
                column: "OrderId",
                unique: true);
        }
    }
}
