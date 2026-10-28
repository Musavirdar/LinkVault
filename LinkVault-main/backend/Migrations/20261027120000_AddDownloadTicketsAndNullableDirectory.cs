using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkVaultApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadTicketsAndNullableDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make Resource.DirectoryId nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "DirectoryId",
                table: "Resources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Add OrganizationId to ShareLinks
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ShareLinks",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            // Backfill OrganizationId from joined Resources
            migrationBuilder.Sql(@"
                UPDATE ""ShareLinks"" sl
                SET ""OrganizationId"" = r.""OrganizationId""
                FROM ""Resources"" r
                WHERE sl.""ResourceId"" = r.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_OrganizationId",
                table: "ShareLinks",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShareLinks_Organizations_OrganizationId",
                table: "ShareLinks",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Create DownloadTickets table
            migrationBuilder.CreateTable(
                name: "DownloadTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownloadTickets_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DownloadTickets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadTickets_Token",
                table: "DownloadTickets",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DownloadTickets_ExpiresAtUtc_IsUsed",
                table: "DownloadTickets",
                columns: new[] { "ExpiresAtUtc", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadTickets_ResourceId",
                table: "DownloadTickets",
                column: "ResourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DownloadTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareLinks_Organizations_OrganizationId",
                table: "ShareLinks");

            migrationBuilder.DropIndex(
                name: "IX_ShareLinks_OrganizationId",
                table: "ShareLinks");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ShareLinks");

            migrationBuilder.AlterColumn<Guid>(
                name: "DirectoryId",
                table: "Resources",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
