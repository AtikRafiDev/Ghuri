using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DestinationPhotoGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Destinations_FileObjects_ImageFileId",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropIndex(
                name: "IX_Destinations_ImageFileId",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "ImageFileId",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.CreateTable(
                name: "DestinationImages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestinationImages_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalSchema: "catalog",
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestinationImages_FileObjects_FileId",
                        column: x => x.FileId,
                        principalSchema: "ops",
                        principalTable: "FileObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DestinationImages_DestinationId",
                schema: "catalog",
                table: "DestinationImages",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_DestinationImages_FileId",
                schema: "catalog",
                table: "DestinationImages",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DestinationImages",
                schema: "catalog");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageFileId",
                schema: "catalog",
                table: "Destinations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_ImageFileId",
                schema: "catalog",
                table: "Destinations",
                column: "ImageFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Destinations_FileObjects_ImageFileId",
                schema: "catalog",
                table: "Destinations",
                column: "ImageFileId",
                principalSchema: "ops",
                principalTable: "FileObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
