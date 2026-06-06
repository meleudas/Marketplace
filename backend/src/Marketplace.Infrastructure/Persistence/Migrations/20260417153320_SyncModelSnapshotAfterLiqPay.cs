using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Historical no-op: schema was consolidated into
    /// <see cref="AddCompanyContractsAndCommissions"/> and <see cref="AddPaymentsAndRefunds"/>.
    /// Keeping the migration id preserves existing __EFMigrationsHistory rows.
    /// </remarks>
    public partial class SyncModelSnapshotAfterLiqPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
