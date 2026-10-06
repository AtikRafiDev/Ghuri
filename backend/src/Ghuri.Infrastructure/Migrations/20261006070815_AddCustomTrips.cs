using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "CustomTripNoSequence",
                schema: "booking",
                startValue: 1001L);

            migrationBuilder.CreateTable(
                name: "CustomTrips",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripNo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalNights = table.Column<byte>(type: "tinyint", nullable: false),
                    Adults = table.Column<byte>(type: "tinyint", nullable: false),
                    Children = table.Column<byte>(type: "tinyint", nullable: false),
                    Infants = table.Column<byte>(type: "tinyint", nullable: false),
                    HotelLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    BudgetPerPerson = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuoteVersion = table.Column<int>(type: "int", nullable: false),
                    QuoteItinerary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    QuoteTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    QuotedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuotedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuoteExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CustomTrips", x => x.Id);
                    table.CheckConstraint("CK_CustomTrips_Adults", "[Adults] >= 1");
                    table.CheckConstraint("CK_CustomTrips_Dates", "[EndDate] > [StartDate]");
                    table.CheckConstraint("CK_CustomTrips_Status", "[Status] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_CustomTrips_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTrips_Users_QuotedBy",
                        column: x => x.QuotedBy,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomTripLegs",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<byte>(type: "tinyint", nullable: false),
                    DestinationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nights = table.Column<byte>(type: "tinyint", nullable: false),
                    TransferToNext = table.Column<byte>(type: "tinyint", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTripLegs", x => x.Id);
                    table.CheckConstraint("CK_CustomTripLegs_Nights", "[Nights] >= 1");
                    table.ForeignKey(
                        name: "FK_CustomTripLegs_CustomTrips_CustomTripId",
                        column: x => x.CustomTripId,
                        principalSchema: "booking",
                        principalTable: "CustomTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomTripLegs_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalSchema: "catalog",
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomTripQuoteLines",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<byte>(type: "tinyint", nullable: false),
                    Category = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTripQuoteLines", x => x.Id);
                    table.CheckConstraint("CK_CustomTripQuoteLines_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_CustomTripQuoteLines_CustomTrips_CustomTripId",
                        column: x => x.CustomTripId,
                        principalSchema: "booking",
                        principalTable: "CustomTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTripLegs_CustomTripId_Sequence",
                schema: "booking",
                table: "CustomTripLegs",
                columns: new[] { "CustomTripId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomTripLegs_DestinationId",
                schema: "booking",
                table: "CustomTripLegs",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTripQuoteLines_CustomTripId_Sequence",
                schema: "booking",
                table: "CustomTripQuoteLines",
                columns: new[] { "CustomTripId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomTrips_CustomerId",
                schema: "booking",
                table: "CustomTrips",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTrips_QuotedBy",
                schema: "booking",
                table: "CustomTrips",
                column: "QuotedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTrips_QuoteExpiresAtUtc",
                schema: "booking",
                table: "CustomTrips",
                column: "QuoteExpiresAtUtc",
                filter: "[Status] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTrips_Status_SubmittedAtUtc",
                schema: "booking",
                table: "CustomTrips",
                columns: new[] { "Status", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTrips_TripNo",
                schema: "booking",
                table: "CustomTrips",
                column: "TripNo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_CustomTrips_CustomTripId",
                schema: "booking",
                table: "Bookings",
                column: "CustomTripId",
                principalSchema: "booking",
                principalTable: "CustomTrips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_CustomTrips_CustomTripId",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "CustomTripLegs",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "CustomTripQuoteLines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "CustomTrips",
                schema: "booking");

            migrationBuilder.DropSequence(
                name: "CustomTripNoSequence",
                schema: "booking");
        }
    }
}
