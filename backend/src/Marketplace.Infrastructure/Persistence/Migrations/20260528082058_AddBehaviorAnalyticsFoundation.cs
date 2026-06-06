using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBehaviorAnalyticsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "behavior_daily_aggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    EventType = table.Column<short>(type: "smallint", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behavior_daily_aggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "behavior_events_dedup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<short>(type: "smallint", nullable: false),
                    BucketStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behavior_events_dedup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "behavior_events_raw",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<short>(type: "smallint", nullable: false),
                    EventVersion = table.Column<short>(type: "smallint", nullable: false),
                    EventKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behavior_events_raw", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "search_query_aggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Query = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_query_aggregates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_behavior_daily_aggregates_Date_EventType",
                table: "behavior_daily_aggregates",
                columns: new[] { "Date", "EventType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_behavior_events_dedup_EventKey_EventType_BucketStartedAtUtc",
                table: "behavior_events_dedup",
                columns: new[] { "EventKey", "EventType", "BucketStartedAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_behavior_events_raw_EventKey",
                table: "behavior_events_raw",
                column: "EventKey");

            migrationBuilder.CreateIndex(
                name: "IX_behavior_events_raw_EventType_OccurredAtUtc",
                table: "behavior_events_raw",
                columns: new[] { "EventType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_search_query_aggregates_Date_Query",
                table: "search_query_aggregates",
                columns: new[] { "Date", "Query" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "behavior_daily_aggregates");

            migrationBuilder.DropTable(
                name: "behavior_events_dedup");

            migrationBuilder.DropTable(
                name: "behavior_events_raw");

            migrationBuilder.DropTable(
                name: "search_query_aggregates");
        }
    }
}
