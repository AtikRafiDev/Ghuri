using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeoDescription",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                schema: "cms",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                schema: "cms",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                schema: "catalog",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                schema: "cms",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                schema: "cms",
                table: "BlogPosts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                schema: "catalog",
                table: "TourPackages",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                schema: "catalog",
                table: "TourPackages",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                schema: "cms",
                table: "Pages",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                schema: "cms",
                table: "Pages",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                schema: "catalog",
                table: "Destinations",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                schema: "catalog",
                table: "Destinations",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                schema: "cms",
                table: "BlogPosts",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                schema: "cms",
                table: "BlogPosts",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);
        }
    }
}
