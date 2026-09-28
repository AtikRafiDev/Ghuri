using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "PackageCodeSequence",
                schema: "catalog",
                startValue: 1001L);

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Icon = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Destinations",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryId = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Destinations_Countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "catalog",
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPackages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DestinationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "varchar(220)", unicode: false, maxLength: 220, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationDays = table.Column<byte>(type: "tinyint", nullable: false),
                    DurationNights = table.Column<byte>(type: "tinyint", nullable: false),
                    TourType = table.Column<byte>(type: "tinyint", nullable: false),
                    Inclusions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exclusions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsAndPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinAge = table.Column<byte>(type: "tinyint", nullable: true),
                    PriceFrom = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    AvgRating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackages", x => x.Id);
                    table.CheckConstraint("CK_TourPackages_DurationDays", "[DurationDays] BETWEEN 1 AND 60");
                    table.CheckConstraint("CK_TourPackages_Status", "[Status] IN (1,2,3)");
                    table.CheckConstraint("CK_TourPackages_TourType", "[TourType] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_TourPackages_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalSchema: "catalog",
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departures",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AdultPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChildPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InfantPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SingleSupplement = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalSeats = table.Column<short>(type: "smallint", nullable: false),
                    ReservedSeats = table.Column<short>(type: "smallint", nullable: false),
                    BookingCutoffDays = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departures", x => x.Id);
                    table.CheckConstraint("CK_Departures_EndDate", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_Departures_PricesNonNegative", "[AdultPrice] >= 0 AND [ChildPrice] >= 0 AND [InfantPrice] >= 0");
                    table.CheckConstraint("CK_Departures_ReservedSeats", "[ReservedSeats] <= [TotalSeats]");
                    table.CheckConstraint("CK_Departures_Status", "[Status] IN (1,2,3,4)");
                    table.CheckConstraint("CK_Departures_TotalSeats", "[TotalSeats] > 0");
                    table.ForeignKey(
                        name: "FK_Departures_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItineraryDays",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNo = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Meals = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Accommodation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryDays_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageAddOns",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricingUnit = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageAddOns", x => x.Id);
                    table.CheckConstraint("CK_PackageAddOns_Price", "[Price] >= 0");
                    table.CheckConstraint("CK_PackageAddOns_PricingUnit", "[PricingUnit] IN (1,2)");
                    table.ForeignKey(
                        name: "FK_PackageAddOns_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageCategories",
                schema: "catalog",
                columns: table => new
                {
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageCategories", x => new { x.PackageId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PackageCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageCategories_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageImages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageImages_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Departures_PackageId_StartDate",
                schema: "catalog",
                table: "Departures",
                columns: new[] { "PackageId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departures_StartDate",
                schema: "catalog",
                table: "Departures",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_Status",
                schema: "catalog",
                table: "Departures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_CountryId",
                schema: "catalog",
                table: "Destinations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_IsFeatured",
                schema: "catalog",
                table: "Destinations",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Slug",
                schema: "catalog",
                table: "Destinations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryDays_PackageId_DayNo",
                schema: "catalog",
                table: "ItineraryDays",
                columns: new[] { "PackageId", "DayNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageAddOns_PackageId",
                schema: "catalog",
                table: "PackageAddOns",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCategories_CategoryId",
                schema: "catalog",
                table: "PackageCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageImages_PackageId",
                schema: "catalog",
                table: "PackageImages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageImages_PackageId_OneCover",
                schema: "catalog",
                table: "PackageImages",
                column: "PackageId",
                unique: true,
                filter: "[IsCover] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_DestinationId",
                schema: "catalog",
                table: "TourPackages",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_IsFeatured",
                schema: "catalog",
                table: "TourPackages",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_PackageCode",
                schema: "catalog",
                table: "TourPackages",
                column: "PackageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_PriceFrom",
                schema: "catalog",
                table: "TourPackages",
                column: "PriceFrom");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_Slug",
                schema: "catalog",
                table: "TourPackages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_Status",
                schema: "catalog",
                table: "TourPackages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Departures",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ItineraryDays",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "PackageAddOns",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "PackageCategories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "PackageImages",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "TourPackages",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Destinations",
                schema: "catalog");

            migrationBuilder.DropSequence(
                name: "PackageCodeSequence",
                schema: "catalog");
        }
    }
}
