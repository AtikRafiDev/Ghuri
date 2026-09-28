using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghuri.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "support");

            migrationBuilder.CreateSequence<int>(
                name: "CustomTourRequestNoSequence",
                schema: "support",
                startValue: 1001L);

            migrationBuilder.CreateTable(
                name: "ContactMessages",
                schema: "support",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                    table.CheckConstraint("CK_ContactMessages_Status", "[Status] IN (1,2,3)");
                });

            migrationBuilder.CreateTable(
                name: "CustomTourRequests",
                schema: "support",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DestinationText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreferredStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Days = table.Column<byte>(type: "tinyint", nullable: true),
                    Adults = table.Column<byte>(type: "tinyint", nullable: false),
                    Children = table.Column<byte>(type: "tinyint", nullable: false),
                    BudgetPerPerson = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    AssignedTo = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTourRequests", x => x.Id);
                    table.CheckConstraint("CK_CustomTourRequests_Status", "[Status] IN (1,2,3,4,5)");
                    table.ForeignKey(
                        name: "FK_CustomTourRequests_Users_AssignedTo",
                        column: x => x.AssignedTo,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTourRequests_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "iam",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomTourRequests_AssignedTo",
                schema: "support",
                table: "CustomTourRequests",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTourRequests_RequestNo",
                schema: "support",
                table: "CustomTourRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomTourRequests_Status",
                schema: "support",
                table: "CustomTourRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTourRequests_UserId",
                schema: "support",
                table: "CustomTourRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMessages",
                schema: "support");

            migrationBuilder.DropTable(
                name: "CustomTourRequests",
                schema: "support");

            migrationBuilder.DropSequence(
                name: "CustomTourRequestNoSequence",
                schema: "support");
        }
    }
}
