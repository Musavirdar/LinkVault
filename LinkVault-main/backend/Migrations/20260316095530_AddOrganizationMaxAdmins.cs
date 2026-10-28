using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkVaultApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMaxAdmins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAdmins",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "MaxAdmins", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3258), 3, new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3258) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3462), new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3462) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3477), new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3478) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3489), new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3489) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3498), new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3499) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAdmins",
                table: "Organizations");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(331), new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(331) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(554), new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(554) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(568), new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(568) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(578), new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(578) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(589), new DateTime(2026, 2, 28, 10, 22, 37, 472, DateTimeKind.Utc).AddTicks(589) });
        }
    }
}
