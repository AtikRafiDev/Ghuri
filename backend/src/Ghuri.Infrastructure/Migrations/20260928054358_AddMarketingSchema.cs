using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketing");

            migrationBuilder.CreateTable(
                name: "Coupons",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DiscountType = table.Column<byte>(type: "tinyint", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: true),
                    MaxUsesPerUser = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                    table.CheckConstraint("CK_Coupons_DiscountType", "[DiscountType] IN (1,2)");
                    table.CheckConstraint("CK_Coupons_ValidToUtc", "[ValidToUtc] > [ValidFromUtc]");
                    table.CheckConstraint("CK_Coupons_Value", "[Value] > 0");
                    table.ForeignKey(
                        name: "FK_Coupons_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ModeratedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModeratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.CheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_Reviews_Status", "[Status] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_ModeratedBy",
                        column: x => x.ModeratedBy,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wishlists",
                schema: "marketing",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlists", x => new { x.UserId, x.PackageId });
                    table.ForeignKey(
                        name: "FK_Wishlists_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wishlists_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponRedemptions",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RedeemedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouponRedemptions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CouponRedemptions_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalSchema: "marketing",
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CouponRedemptions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CouponId",
                schema: "booking",
                table: "Bookings",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_BookingId",
                schema: "marketing",
                table: "CouponRedemptions",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_CouponId_UserId",
                schema: "marketing",
                table: "CouponRedemptions",
                columns: new[] { "CouponId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_UserId",
                schema: "marketing",
                table: "CouponRedemptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                schema: "marketing",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_PackageId",
                schema: "marketing",
                table: "Coupons",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                schema: "marketing",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ModeratedBy",
                schema: "marketing",
                table: "Reviews",
                column: "ModeratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PackageId",
                schema: "marketing",
                table: "Reviews",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                schema: "marketing",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_PackageId",
                schema: "marketing",
                table: "Wishlists",
                column: "PackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Coupons_CouponId",
                schema: "booking",
                table: "Bookings",
                column: "CouponId",
                principalSchema: "marketing",
                principalTable: "Coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Coupons_CouponId",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "CouponRedemptions",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Reviews",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Wishlists",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Coupons",
                schema: "marketing");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CouponId",
                schema: "booking",
                table: "Bookings");
        }
    }
}
