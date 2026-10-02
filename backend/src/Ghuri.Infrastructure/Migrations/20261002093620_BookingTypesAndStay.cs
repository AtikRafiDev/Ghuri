using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingTypesAndStay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PackageId",
                schema: "booking",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartureId",
                schema: "booking",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<byte>(
                name: "BookingType",
                schema: "booking",
                table: "Bookings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomTripId",
                schema: "booking",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                schema: "booking",
                table: "Bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<byte>(
                name: "Nights",
                schema: "booking",
                table: "Bookings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                schema: "booking",
                table: "Bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomTripId",
                schema: "booking",
                table: "Bookings",
                column: "CustomTripId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StartDate",
                schema: "booking",
                table: "Bookings",
                column: "StartDate");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_Dates",
                schema: "booking",
                table: "Bookings",
                sql: "[EndDate] >= [StartDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_Shape",
                schema: "booking",
                table: "Bookings",
                sql: "([BookingType] = 1 AND [DepartureId] IS NOT NULL AND [PackageId] IS NOT NULL AND [CustomTripId] IS NULL) OR ([BookingType] = 2 AND [DepartureId] IS NULL AND [PackageId] IS NOT NULL AND [CustomTripId] IS NULL AND [Nights] >= 1) OR ([BookingType] = 3 AND [DepartureId] IS NULL AND [PackageId] IS NULL AND [CustomTripId] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomTripId",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StartDate",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_Dates",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_Shape",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingType",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomTripId",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Nights",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "PackageId",
                schema: "booking",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartureId",
                schema: "booking",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
