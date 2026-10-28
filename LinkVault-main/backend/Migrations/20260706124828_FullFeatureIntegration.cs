using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkVaultApi.Migrations
{
    /// <inheritdoc />
    public partial class FullFeatureIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicPageSlug",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialLinks",
                table: "Users",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChecksumSHA256",
                table: "Resources",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DownloadCount",
                table: "Resources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VersionCount",
                table: "Resources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "MaxFileSizeBytes",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StorageLimitBytes",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StorageUsedBytes",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "AnimationStyle",
                table: "Links",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeLock",
                table: "Links",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbedHtml",
                table: "Links",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "Links",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LinkType",
                table: "Links",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "Links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSHA256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ChangeNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileVersions_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileVersions_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileThemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BackgroundType = table.Column<int>(type: "integer", nullable: false),
                    BackgroundValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ButtonStyle = table.Column<int>(type: "integer", nullable: false),
                    ButtonColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ButtonTextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ButtonHoverColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    FontFamily = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProfileLayout = table.Column<int>(type: "integer", nullable: false),
                    SocialLinksPosition = table.Column<int>(type: "integer", nullable: false),
                    CustomCSS = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    HideBranding = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileThemes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxDownloads = table.Column<int>(type: "integer", nullable: true),
                    CurrentDownloads = table.Column<int>(type: "integer", nullable: false),
                    AllowPreview = table.Column<bool>(type: "boolean", nullable: false),
                    RequireEmail = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareLinks_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareLinks_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileCount = table.Column<int>(type: "integer", nullable: false),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    MaxDownloads = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClickEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShareLinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Referrer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Browser = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OS = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClickEvents_Links_LinkId",
                        column: x => x.LinkId,
                        principalTable: "Links",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClickEvents_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClickEvents_ShareLinks_ShareLinkId",
                        column: x => x.ShareLinkId,
                        principalTable: "ShareLinks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransferFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferFiles_Transfers_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "MaxFileSizeBytes", "StorageLimitBytes", "StorageUsedBytes", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3129), 26214400L, 1073741824L, 0L, new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3130) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3297), new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3297) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3314), new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3314) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3328), new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3328) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3340), new DateTime(2026, 7, 6, 12, 48, 28, 268, DateTimeKind.Utc).AddTicks(3340) });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PublicPageSlug",
                table: "Users",
                column: "PublicPageSlug",
                unique: true,
                filter: "\"PublicPageSlug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Links_UserId_LinkType",
                table: "Links",
                columns: new[] { "UserId", "LinkType" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_LinkId_Timestamp",
                table: "ClickEvents",
                columns: new[] { "LinkId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ResourceId_Timestamp",
                table: "ClickEvents",
                columns: new[] { "ResourceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_ShareLinkId",
                table: "ClickEvents",
                column: "ShareLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_CreatedById",
                table: "FileVersions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_ResourceId_VersionNumber",
                table: "FileVersions",
                columns: new[] { "ResourceId", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileThemes_UserId",
                table: "ProfileThemes",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_CreatedById",
                table: "ShareLinks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_ResourceId",
                table: "ShareLinks",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_Token",
                table: "ShareLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferFiles_TransferId",
                table: "TransferFiles",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_CreatedByUserId",
                table: "Transfers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_IsActive_ExpiresAt",
                table: "Transfers",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_Token",
                table: "Transfers",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClickEvents");

            migrationBuilder.DropTable(
                name: "FileVersions");

            migrationBuilder.DropTable(
                name: "ProfileThemes");

            migrationBuilder.DropTable(
                name: "TransferFiles");

            migrationBuilder.DropTable(
                name: "ShareLinks");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Users_PublicPageSlug",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Links_UserId_LinkType",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "PublicPageSlug",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SocialLinks",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChecksumSHA256",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "DownloadCount",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "VersionCount",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "MaxFileSizeBytes",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StorageLimitBytes",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StorageUsedBytes",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "AnimationStyle",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "CodeLock",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "EmbedHtml",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "LinkType",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "Links");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3258), new DateTime(2026, 3, 16, 9, 55, 29, 535, DateTimeKind.Utc).AddTicks(3258) });

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
    }
}
