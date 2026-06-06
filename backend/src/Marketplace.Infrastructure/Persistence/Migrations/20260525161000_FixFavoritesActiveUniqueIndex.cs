using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    public partial class FixFavoritesActiveUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_favorites_UserId_ProductId",
                table: "favorites");

            migrationBuilder.CreateIndex(
                name: "IX_favorites_UserId_ProductId",
                table: "favorites",
                columns: new[] { "UserId", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_favorites_UserId_ProductId",
                table: "favorites");

            migrationBuilder.CreateIndex(
                name: "IX_favorites_UserId_ProductId",
                table: "favorites",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }
    }
}
