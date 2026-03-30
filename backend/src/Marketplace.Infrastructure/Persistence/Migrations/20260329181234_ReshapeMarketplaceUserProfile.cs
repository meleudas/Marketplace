using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReshapeMarketplaceUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_marketplace_users_Email",
                table: "marketplace_users");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_users_IdentityUserId",
                table: "marketplace_users");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_users_UserName",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "PhoneVerified",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "Roles",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "marketplace_users");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "marketplace_users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "marketplace_users",
                newName: "Birthday");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "marketplace_users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "marketplace_users",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "marketplace_users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "marketplace_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "marketplace_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "marketplace_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationDocument",
                table: "marketplace_users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_users_FirstName",
                table: "marketplace_users",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_users_LastName",
                table: "marketplace_users",
                column: "LastName");

            migrationBuilder.AddForeignKey(
                name: "FK_marketplace_users_AspNetUsers_Id",
                table: "marketplace_users",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_marketplace_users_AspNetUsers_Id",
                table: "marketplace_users");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_users_FirstName",
                table: "marketplace_users");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_users_LastName",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "marketplace_users");

            migrationBuilder.DropColumn(
                name: "VerificationDocument",
                table: "marketplace_users");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "marketplace_users",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "Birthday",
                table: "marketplace_users",
                newName: "DeletedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "marketplace_users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "marketplace_users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "marketplace_users",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityUserId",
                table: "marketplace_users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "marketplace_users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneVerified",
                table: "marketplace_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Roles",
                table: "marketplace_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "marketplace_users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_users_Email",
                table: "marketplace_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_users_IdentityUserId",
                table: "marketplace_users",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_users_UserName",
                table: "marketplace_users",
                column: "UserName",
                unique: true);
        }
    }
}
