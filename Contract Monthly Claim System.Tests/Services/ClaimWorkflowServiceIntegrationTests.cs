using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Tests.Services
{
    public class ClaimWorkflowServiceIntegrationTests
    {
        [Fact]
        public async Task ApproveByCoordinatorAsync_UpdatesClaimAndWritesAuditRecord()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await using var context = await CreateContextAsync(connection);
            var service = new ClaimWorkflowService(context);

            var result = await service.ApproveByCoordinatorAsync(1, 3, "  Supporting documents checked  ");

            var claim = await context.Claims.FirstAsync(c => c.claimId == 1);
            var review = await context.ClaimReviews.SingleAsync(r => r.claimId == 1);

            Assert.True(result.Success);
            Assert.Equal((int)ClaimStatusType.ApprovedByCoordinator, claim.statusId);
            Assert.Equal(3, review.reviewerUserId);
            Assert.Equal(nameof(UserRoleType.Coordinator), review.reviewerRole);
            Assert.Equal(nameof(ClaimReviewAction.ApprovedByCoordinator), review.action);
            Assert.Equal((int)ClaimStatusType.Submitted, review.fromStatusId);
            Assert.Equal((int)ClaimStatusType.ApprovedByCoordinator, review.toStatusId);
            Assert.Equal("Supporting documents checked", review.comments);
        }

        [Fact]
        public async Task ApproveByManagerAsync_BlocksSubmittedClaimBeforeCoordinatorReview()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await using var context = await CreateContextAsync(connection);
            var service = new ClaimWorkflowService(context);

            var result = await service.ApproveByManagerAsync(1, 4, "Manager review");

            var claim = await context.Claims.FirstAsync(c => c.claimId == 1);
            var auditRecords = await context.ClaimReviews.Where(r => r.claimId == 1).ToListAsync();

            Assert.False(result.Success);
            Assert.Equal((int)ClaimStatusType.Submitted, claim.statusId);
            Assert.Empty(auditRecords);
        }

        [Fact]
        public async Task RejectByCoordinatorAsync_BlocksReviewCommentsOverLimit()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await using var context = await CreateContextAsync(connection);
            var service = new ClaimWorkflowService(context);
            var longComment = new string('x', 501);

            var result = await service.RejectByCoordinatorAsync(1, 3, longComment);

            var claim = await context.Claims.FirstAsync(c => c.claimId == 1);

            Assert.False(result.Success);
            Assert.Equal((int)ClaimStatusType.Submitted, claim.statusId);
            Assert.False(await context.ClaimReviews.AnyAsync(r => r.claimId == 1));
        }

        private static async Task<ApplicationDbContext> CreateContextAsync(SqliteConnection connection)
        {
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return context;
        }
    }
}
