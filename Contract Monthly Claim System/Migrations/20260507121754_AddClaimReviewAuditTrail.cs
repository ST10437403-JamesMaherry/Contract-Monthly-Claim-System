using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractMonthlyClaimSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimReviewAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimReviews",
                columns: table => new
                {
                    claimReviewId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    claimId = table.Column<int>(type: "INTEGER", nullable: false),
                    reviewerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    reviewerRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    fromStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    toStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    reviewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    comments = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimReviews", x => x.claimReviewId);
                    table.ForeignKey(
                        name: "FK_ClaimReviews_Claims_claimId",
                        column: x => x.claimId,
                        principalTable: "Claims",
                        principalColumn: "claimId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimReviews_Users_reviewerUserId",
                        column: x => x.reviewerUserId,
                        principalTable: "Users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimReviews_claimId",
                table: "ClaimReviews",
                column: "claimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimReviews_reviewerUserId",
                table: "ClaimReviews",
                column: "reviewerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimReviews");
        }
    }
}
