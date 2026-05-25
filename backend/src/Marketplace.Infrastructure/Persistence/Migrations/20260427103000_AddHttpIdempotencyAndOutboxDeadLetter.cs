using System;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260427103000_AddHttpIdempotencyAndOutboxDeadLetter")]
    public partial class AddHttpIdempotencyAndOutboxDeadLetter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeadLetterCategory",
                table: "outbox_messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeadLetterReason",
                table: "outbox_messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "http_idempotency_requests",
                columns: table => new
                {
                    Scope = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseBodyJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_http_idempotency_requests", x => new { x.Scope, x.IdempotencyKey });
                });

            migrationBuilder.CreateIndex(
                name: "IX_http_idempotency_requests_CompletedAtUtc",
                table: "http_idempotency_requests",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_http_idempotency_requests_ExpiresAtUtc",
                table: "http_idempotency_requests",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_DeadLetteredAtUtc",
                table: "outbox_messages",
                column: "DeadLetteredAtUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "http_idempotency_requests");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_DeadLetteredAtUtc",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetterCategory",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAtUtc",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetterReason",
                table: "outbox_messages");
        }
    }
}
