using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "iam",
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { (byte)1, "Full control: settings, staff, reports", "SuperAdmin" },
                    { (byte)2, "Admin staff: packages, departures, bookings, content", "Manager" },
                    { (byte)3, "Admin staff: bookings and customers", "Sales" },
                    { (byte)4, "Admin staff: payments and refunds", "Accounts" },
                    { (byte)5, "Searches, books and pays for tours", "Customer" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "iam",
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)1);

            migrationBuilder.DeleteData(
                schema: "iam",
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)2);

            migrationBuilder.DeleteData(
                schema: "iam",
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)3);

            migrationBuilder.DeleteData(
                schema: "iam",
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)4);

            migrationBuilder.DeleteData(
                schema: "iam",
                table: "Roles",
                keyColumn: "Id",
                keyValue: (byte)5);
        }
    }
}
