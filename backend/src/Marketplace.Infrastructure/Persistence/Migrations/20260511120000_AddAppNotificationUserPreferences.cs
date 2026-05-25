using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260511120000_AddAppNotificationUserPreferences")]
public partial class AddAppNotificationUserPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "NotifyAppByEmail",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "NotifyAppByTelegram",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NotifyAppByEmail",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "NotifyAppByTelegram",
            table: "AspNetUsers");
    }
}
