using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContractMonthlyClaimSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimStatuses",
                columns: table => new
                {
                    statusId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    statusName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimStatuses", x => x.statusId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    userId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    firstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    lastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    phoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    userRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    hourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    passwordHash = table.Column<string>(type: "TEXT", nullable: false),
                    passwordSalt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.userId);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    claimId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    userId = table.Column<int>(type: "INTEGER", nullable: false),
                    hoursWorked = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    hourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    totalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    statusId = table.Column<int>(type: "INTEGER", nullable: false),
                    submissionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.claimId);
                    table.ForeignKey(
                        name: "FK_Claims_ClaimStatuses_statusId",
                        column: x => x.statusId,
                        principalTable: "ClaimStatuses",
                        principalColumn: "statusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Users_userId",
                        column: x => x.userId,
                        principalTable: "Users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    documentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    claimId = table.Column<int>(type: "INTEGER", nullable: false),
                    fileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    uploadDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fileType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    fileSize = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.documentId);
                    table.ForeignKey(
                        name: "FK_Documents_Claims_claimId",
                        column: x => x.claimId,
                        principalTable: "Claims",
                        principalColumn: "claimId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ClaimStatuses",
                columns: new[] { "statusId", "statusName" },
                values: new object[,]
                {
                    { 1, "Submitted" },
                    { 2, "Approved by Coordinator" },
                    { 3, "Approved by Manager" },
                    { 4, "Rejected by Coordinator" },
                    { 5, "Rejected by Manager" },
                    { 6, "Paid" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "userId", "email", "firstName", "hourlyRate", "lastName", "passwordHash", "passwordSalt", "phoneNumber", "userRole" },
                values: new object[,]
                {
                    { 1, "mattjones@university.co.za", "Matt", 150.00m, "Jones", "U035CuWx4TAQr1bQABL5SBFf+/E/MdO5GHdS5+KFixo=", "hhyaO083w0ZOYl55AuCG6jBRlblbvQ0FkyYJrrd4/UM=", "+27 11 123 4567", "Lecturer" },
                    { 2, "crownvic@university.co.za", "Victoria", 175.00m, "Crown", "eeIyLs4GRBSyrsiCs2eyLL0wu1PGBo5NLxFD5uctAGw=", "FwLcBNqyRwyo3HwgYLfsx/vWhQTKGz7fLBFPOZJEy3A=", "+27 11 123 4568", "Lecturer" },
                    { 3, "sarahw@university.co.za", "Sarah", 200.00m, "Wilson", "DP5tg9KEDgPKsmtBD/+0BlazsXNLxPdAwy0lbREzo+4=", "QXk1gJdTSHSwRihIMCTs9QZ/cHBX0aIIHGmmuDQKnCY=", "+27 11 123 4569", "Coordinator" },
                    { 4, "davidb@university.co.za", "David", 250.00m, "Brown", "0mgrLlw+wB23c/ol/jHPBCNtNBljsLeZcG8Di0u6WnY=", "lrQmJEDmwIAtsXwatTZyVNiOm9/ifk9mpEh20UGgxPE=", "+27 11 123 4570", "Manager" },
                    { 5, "hr@university.co.za", "HR", 0.00m, "Administrator", "NWbrGUAGNAw7bpYGgmO7vvznpvo5zNRXjubj5AHNNCQ=", "Ui+5ik/IJcVUCX8VbUJ8vFDZBblifaW3gIq8FWQxKD0=", "+27 11 123 4571", "HR" }
                });

            migrationBuilder.InsertData(
                table: "Claims",
                columns: new[] { "claimId", "Notes", "hourlyRate", "hoursWorked", "statusId", "submissionDate", "totalAmount", "userId" },
                values: new object[,]
                {
                    { 1, "Regular teaching hours", 150m, 20m, 1, new DateTime(2024, 10, 30, 12, 0, 0, 0, DateTimeKind.Unspecified), 3000m, 1 },
                    { 2, "Extra marking hours", 150m, 15m, 2, new DateTime(2024, 10, 27, 12, 0, 0, 0, DateTimeKind.Unspecified), 2250m, 1 },
                    { 3, "Research supervision", 175m, 18m, 3, new DateTime(2024, 10, 31, 12, 0, 0, 0, DateTimeKind.Unspecified), 3150m, 2 },
                    { 4, "Exam preparation", 175m, 12m, 4, new DateTime(2024, 10, 29, 12, 0, 0, 0, DateTimeKind.Unspecified), 2100m, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_statusId",
                table: "Claims",
                column: "statusId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_userId",
                table: "Claims",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_claimId",
                table: "Documents",
                column: "claimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "ClaimStatuses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
