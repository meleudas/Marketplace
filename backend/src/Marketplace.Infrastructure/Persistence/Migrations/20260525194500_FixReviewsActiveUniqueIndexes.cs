using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    public partial class FixReviewsActiveUniqueIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_reviews_ProductId_UserId",
                table: "product_reviews");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_ProductId_UserId",
                table: "product_reviews",
                columns: new[] { "ProductId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.DropIndex(
                name: "IX_company_reviews_CompanyId_UserId",
                table: "company_reviews");

            migrationBuilder.CreateIndex(
                name: "IX_company_reviews_CompanyId_UserId",
                table: "company_reviews",
                columns: new[] { "CompanyId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_reviews_ProductId_UserId",
                table: "product_reviews");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_ProductId_UserId",
                table: "product_reviews",
                columns: new[] { "ProductId", "UserId" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_company_reviews_CompanyId_UserId",
                table: "company_reviews");

            migrationBuilder.CreateIndex(
                name: "IX_company_reviews_CompanyId_UserId",
                table: "company_reviews",
                columns: new[] { "CompanyId", "UserId" },
                unique: true);
        }
    }
}
