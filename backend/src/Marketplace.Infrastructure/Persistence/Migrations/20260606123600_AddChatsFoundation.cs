using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Attachments = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "jsonb", nullable: false),
                    ReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_moderation_actions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: true),
                    ActionType = table.Column<short>(type: "smallint", nullable: false),
                    ModeratorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_moderation_actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_participants",
                columns: table => new
                {
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<short>(type: "smallint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_participants", x => new { x.ChatId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "chat_read_states",
                columns: table => new
                {
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastReadMessageId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_read_states", x => new { x.ChatId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "chats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    ProductId = table.Column<long>(type: "bigint", nullable: true),
                    LastMessageText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastMessageSenderId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMessageCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Meta = table.Column<string>(type: "jsonb", nullable: false),
                    ParticipantsSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ChatId_CreatedAt",
                table: "chat_messages",
                columns: new[] { "ChatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_SenderId",
                table: "chat_messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_moderation_actions_ChatId_CreatedAt",
                table: "chat_moderation_actions",
                columns: new[] { "ChatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_ChatId_IsDeleted_LeftAt",
                table: "chat_participants",
                columns: new[] { "ChatId", "IsDeleted", "LeftAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_UserId",
                table: "chat_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_chats_OrderId",
                table: "chats",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_chats_ProductId",
                table: "chats",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_chats_Type_InitiatorUserId_Status",
                table: "chats",
                columns: new[] { "Type", "InitiatorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_chats_Type_OrderId_InitiatorUserId_Status",
                table: "chats",
                columns: new[] { "Type", "OrderId", "InitiatorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_chats_Type_ProductId_InitiatorUserId_Status",
                table: "chats",
                columns: new[] { "Type", "ProductId", "InitiatorUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_moderation_actions");

            migrationBuilder.DropTable(
                name: "chat_participants");

            migrationBuilder.DropTable(
                name: "chat_read_states");

            migrationBuilder.DropTable(
                name: "chats");
        }
    }
}
