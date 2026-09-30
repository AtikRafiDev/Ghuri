using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogUniqueIndexesIgnoreDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Destinations_Slug",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Slug",
                schema: "catalog",
                table: "Destinations",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "catalog",
                table: "Categories",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Destinations_Slug",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Slug",
                schema: "catalog",
                table: "Destinations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "catalog",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories",
                column: "Slug",
                unique: true);
        }
    }
}
