using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateSequence<int>(
                name: "BookingNoSequence",
                schema: "booking",
                startValue: 100001L);

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingNo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Adults = table.Column<byte>(type: "tinyint", nullable: false),
                    Children = table.Column<byte>(type: "tinyint", nullable: false),
                    Infants = table.Column<byte>(type: "tinyint", nullable: false),
                    AdultPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChildPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InfantPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AddOnTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    PaymentPlan = table.Column<byte>(type: "tinyint", nullable: false),
                    BalanceDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    HoldExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SpecialRequest = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Source = table.Column<byte>(type: "tinyint", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.CheckConstraint("CK_Bookings_Adults", "[Adults] >= 1");
                    table.CheckConstraint("CK_Bookings_PaidAmount", "[PaidAmount] <= [TotalAmount]");
                    table.CheckConstraint("CK_Bookings_PaymentPlan", "[PaymentPlan] IN (1,2)");
                    table.CheckConstraint("CK_Bookings_Source", "[Source] IN (1,2)");
                    table.CheckConstraint("CK_Bookings_Status", "[Status] IN (1,2,3,4,5,6)");
                    table.CheckConstraint("CK_Bookings_TotalAmount", "[TotalAmount] = [SubTotal] + [AddOnTotal] - [DiscountAmount]");
                    table.ForeignKey(
                        name: "FK_Bookings_Departures_DepartureId",
                        column: x => x.DepartureId,
                        principalSchema: "catalog",
                        principalTable: "Departures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CancellationPolicies",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinDaysBefore = table.Column<short>(type: "smallint", nullable: false),
                    RefundPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationPolicies", x => x.Id);
                    table.CheckConstraint("CK_CancellationPolicies_RefundPercent", "[RefundPercent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_CancellationPolicies_TourPackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "catalog",
                        principalTable: "TourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingAddOns",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddOnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<short>(type: "smallint", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAddOns", x => x.Id);
                    table.CheckConstraint("CK_BookingAddOns_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_BookingAddOns_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingAddOns_PackageAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalSchema: "catalog",
                        principalTable: "PackageAddOns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingStatusHistory",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    ToStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistory_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingTravellers",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TravellerType = table.Column<byte>(type: "tinyint", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Gender = table.Column<byte>(type: "tinyint", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Nationality = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: true),
                    PassportNo = table.Column<byte[]>(type: "varbinary(128)", maxLength: 128, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IsLead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingTravellers", x => x.Id);
                    table.CheckConstraint("CK_BookingTravellers_Gender", "[Gender] IS NULL OR [Gender] IN (1,2,3)");
                    table.CheckConstraint("CK_BookingTravellers_TravellerType", "[TravellerType] IN (1,2,3)");
                    table.ForeignKey(
                        name: "FK_BookingTravellers_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingAddOns_AddOnId",
                schema: "booking",
                table: "BookingAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingAddOns_BookingId",
                schema: "booking",
                table: "BookingAddOns",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingNo",
                schema: "booking",
                table: "Bookings",
                column: "BookingNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId_CreatedAtUtc",
                schema: "booking",
                table: "Bookings",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DepartureId",
                schema: "booking",
                table: "Bookings",
                column: "DepartureId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_HoldExpiresAtUtc",
                schema: "booking",
                table: "Bookings",
                column: "HoldExpiresAtUtc",
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PackageId",
                schema: "booking",
                table: "Bookings",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                schema: "booking",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistory_BookingId",
                schema: "booking",
                table: "BookingStatusHistory",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTravellers_BookingId",
                schema: "booking",
                table: "BookingTravellers",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicies_GlobalDefault_MinDaysBefore",
                schema: "booking",
                table: "CancellationPolicies",
                column: "MinDaysBefore",
                unique: true,
                filter: "[PackageId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicies_PackageId_MinDaysBefore",
                schema: "booking",
                table: "CancellationPolicies",
                columns: new[] { "PackageId", "MinDaysBefore" },
                unique: true,
                filter: "[PackageId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingAddOns",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "BookingStatusHistory",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "BookingTravellers",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "CancellationPolicies",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "booking");

            migrationBuilder.DropSequence(
                name: "BookingNoSequence",
                schema: "booking");
        }
    }
}
