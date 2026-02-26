using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkVault.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageEditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "ChatMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "ChatMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 25, 16, 33, 56, 756, DateTimeKind.Utc).AddTicks(7351));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 25, 16, 33, 56, 756, DateTimeKind.Utc).AddTicks(7355));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "ChatMessages");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 25, 7, 28, 30, 754, DateTimeKind.Utc).AddTicks(5056));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2026, 2, 25, 7, 28, 30, 754, DateTimeKind.Utc).AddTicks(5058));
        }
    }
}
