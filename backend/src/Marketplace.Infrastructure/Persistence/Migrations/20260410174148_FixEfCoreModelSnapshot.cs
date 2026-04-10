using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// No-op schema change: product tables are created in <see cref="AddProductsModule"/>.
    /// This migration exists so <see cref="ApplicationDbContextModelSnapshot"/> matches the current model
    /// and EF Core no longer throws PendingModelChangesWarning on startup.
    /// </summary>
    public partial class FixEfCoreModelSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
