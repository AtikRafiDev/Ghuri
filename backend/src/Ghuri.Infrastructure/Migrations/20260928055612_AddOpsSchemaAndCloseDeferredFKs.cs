using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpsSchemaAndCloseDeferredFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ChangesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileObjects",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileObjects", x => x.Id);
                    table.CheckConstraint("CK_FileObjects_SizeBytes", "[SizeBytes] <= 10485760");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyKeys",
                schema: "ops",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    StatusCode = table.Column<short>(type: "smallint", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyKeys", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<byte>(type: "tinyint", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "ops",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValueType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AvatarFileId",
                schema: "iam",
                table: "Users",
                column: "AvatarFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageImages_FileId",
                schema: "catalog",
                table: "PackageImages",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_ImageFileId",
                schema: "catalog",
                table: "Destinations",
                column: "ImageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_CoverFileId",
                schema: "cms",
                table: "BlogPosts",
                column: "CoverFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_ImageFileId",
                schema: "cms",
                table: "Banners",
                column: "ImageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                schema: "ops",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "ops",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_StorageKey",
                schema: "ops",
                table: "FileObjects",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeys_ExpiresAtUtc",
                schema: "ops",
                table: "IdempotencyKeys",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc",
                schema: "ops",
                table: "OutboxMessages",
                column: "ProcessedAtUtc",
                filter: "[ProcessedAtUtc] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Banners_FileObjects_ImageFileId",
                schema: "cms",
                table: "Banners",
                column: "ImageFileId",
                principalSchema: "ops",
                principalTable: "FileObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BlogPosts_FileObjects_CoverFileId",
                schema: "cms",
                table: "BlogPosts",
                column: "CoverFileId",
                principalSchema: "ops",
                principalTable: "FileObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Destinations_FileObjects_ImageFileId",
                schema: "catalog",
                table: "Destinations",
                column: "ImageFileId",
                principalSchema: "ops",
                principalTable: "FileObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageImages_FileObjects_FileId",
                schema: "catalog",
                table: "PackageImages",
                column: "FileId",
                principalSchema: "ops",
                principalTable: "FileObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_FileObjects_AvatarFileId",
                schema: "iam",
                table: "Users",
                column: "AvatarFileId",
                principalSchema: "ops",
                principalTable: "FileObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Banners_FileObjects_ImageFileId",
                schema: "cms",
                table: "Banners");

            migrationBuilder.DropForeignKey(
                name: "FK_BlogPosts_FileObjects_CoverFileId",
                schema: "cms",
                table: "BlogPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_Destinations_FileObjects_ImageFileId",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageImages_FileObjects_FileId",
                schema: "catalog",
                table: "PackageImages");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_FileObjects_AvatarFileId",
                schema: "iam",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "FileObjects",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "IdempotencyKeys",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_Users_AvatarFileId",
                schema: "iam",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PackageImages_FileId",
                schema: "catalog",
                table: "PackageImages");

            migrationBuilder.DropIndex(
                name: "IX_Destinations_ImageFileId",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_CoverFileId",
                schema: "cms",
                table: "BlogPosts");

            migrationBuilder.DropIndex(
                name: "IX_Banners_ImageFileId",
                schema: "cms",
                table: "Banners");
        }
    }
}
