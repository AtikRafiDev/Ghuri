using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagePricingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PackageImages_PackageId_OneCover",
                schema: "catalog",
                table: "PackageImages");

            migrationBuilder.DropColumn(
                name: "IsCover",
                schema: "catalog",
                table: "PackageImages");

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                schema: "catalog",
                table: "TourPackages",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraNightPrice",
                schema: "catalog",
                table: "TourPackages",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "MaxNights",
                schema: "catalog",
                table: "TourPackages",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "MinLeadDays",
                schema: "catalog",
                table: "TourPackages",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "MinNights",
                schema: "catalog",
                table: "TourPackages",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PricingMode",
                schema: "catalog",
                table: "TourPackages",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TourPackages_FlexibleStay",
                schema: "catalog",
                table: "TourPackages",
                sql: "([PricingMode] = 1 AND [MinNights] IS NULL AND [MaxNights] IS NULL AND [BasePrice] IS NULL AND [ExtraNightPrice] IS NULL AND [MinLeadDays] IS NULL) OR ([PricingMode] = 2 AND [MinNights] IS NOT NULL AND [MaxNights] IS NOT NULL AND [BasePrice] IS NOT NULL AND [ExtraNightPrice] IS NOT NULL AND [MinLeadDays] IS NOT NULL AND [MinNights] >= 1 AND [MaxNights] >= [MinNights] AND [BasePrice] > 0 AND [ExtraNightPrice] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TourPackages_PricingMode",
                schema: "catalog",
                table: "TourPackages",
                sql: "[PricingMode] IN (1,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TourPackages_FlexibleStay",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TourPackages_PricingMode",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "BasePrice",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "ExtraNightPrice",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "MaxNights",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "MinLeadDays",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "MinNights",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "PricingMode",
                schema: "catalog",
                table: "TourPackages");

            migrationBuilder.AddColumn<bool>(
                name: "IsCover",
                schema: "catalog",
                table: "PackageImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PackageImages_PackageId_OneCover",
                schema: "catalog",
                table: "PackageImages",
                column: "PackageId",
                unique: true,
                filter: "[IsCover] = 1");
        }
    }
}
