using System;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260617120000_AddIntegrationRetriesAndOrderCancellation")]
    public partial class AddIntegrationRetriesAndOrderCancellation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "CancellationReasonCode",
                table: "orders",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationComment",
                table: "orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "integration_retries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetterReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeadLetterCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_retries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_integration_retries_DeadLetteredAtUtc",
                table: "integration_retries",
                column: "DeadLetteredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_integration_retries_Kind_AggregateType_AggregateId",
                table: "integration_retries",
                columns: new[] { "Kind", "AggregateType", "AggregateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_retries_NextAttemptAtUtc",
                table: "integration_retries",
                column: "NextAttemptAtUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_retries");

            migrationBuilder.DropColumn(
                name: "CancellationComment",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CancellationReasonCode",
                table: "orders");
        }
    }
}
