using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IClaimWorkflowService
    {
        Task<ClaimWorkflowResult> ApproveByCoordinatorAsync(int claimId, int reviewerUserId, string? comments);
        Task<ClaimWorkflowResult> RejectByCoordinatorAsync(int claimId, int reviewerUserId, string? comments);
        Task<ClaimWorkflowResult> ApproveByManagerAsync(int claimId, int reviewerUserId, string? comments);
        Task<ClaimWorkflowResult> RejectByManagerAsync(int claimId, int reviewerUserId, string? comments);
        Task<ClaimWorkflowResult> MarkAsPaidAsync(int claimId, int reviewerUserId);
    }

    public class ClaimWorkflowResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ClaimWorkflowResult Passed(string message)
        {
            return new ClaimWorkflowResult { Success = true, Message = message };
        }

        public static ClaimWorkflowResult Failed(string message)
        {
            return new ClaimWorkflowResult { Success = false, Message = message };
        }
    }

    public class ClaimWorkflowService : IClaimWorkflowService
    {
        private readonly ApplicationDbContext _context;

        public ClaimWorkflowService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ClaimWorkflowResult> ApproveByCoordinatorAsync(int claimId, int reviewerUserId, string? comments)
        {
            return await MoveClaimAsync(
                claimId,
                reviewerUserId,
                nameof(UserRoleType.Coordinator),
                new[] { ClaimStatusType.Submitted },
                ClaimStatusType.ApprovedByCoordinator,
                ClaimReviewAction.ApprovedByCoordinator,
                comments,
                $"Claim #{claimId} approved successfully!");
        }

        public async Task<ClaimWorkflowResult> RejectByCoordinatorAsync(int claimId, int reviewerUserId, string? comments)
        {
            return await MoveClaimAsync(
                claimId,
                reviewerUserId,
                nameof(UserRoleType.Coordinator),
                new[] { ClaimStatusType.Submitted },
                ClaimStatusType.RejectedByCoordinator,
                ClaimReviewAction.RejectedByCoordinator,
                comments,
                $"Claim #{claimId} rejected!");
        }

        public async Task<ClaimWorkflowResult> ApproveByManagerAsync(int claimId, int reviewerUserId, string? comments)
        {
            return await MoveClaimAsync(
                claimId,
                reviewerUserId,
                nameof(UserRoleType.Manager),
                new[] { ClaimStatusType.ApprovedByCoordinator, ClaimStatusType.RejectedByCoordinator },
                ClaimStatusType.ApprovedByManager,
                ClaimReviewAction.ApprovedByManager,
                comments,
                $"Claim #{claimId} approved! Ready for payment.");
        }

        public async Task<ClaimWorkflowResult> RejectByManagerAsync(int claimId, int reviewerUserId, string? comments)
        {
            return await MoveClaimAsync(
                claimId,
                reviewerUserId,
                nameof(UserRoleType.Manager),
                new[] { ClaimStatusType.ApprovedByCoordinator, ClaimStatusType.RejectedByCoordinator },
                ClaimStatusType.RejectedByManager,
                ClaimReviewAction.RejectedByManager,
                comments,
                $"Claim #{claimId} rejected!");
        }

        public async Task<ClaimWorkflowResult> MarkAsPaidAsync(int claimId, int reviewerUserId)
        {
            return await MoveClaimAsync(
                claimId,
                reviewerUserId,
                nameof(UserRoleType.Manager),
                new[] { ClaimStatusType.ApprovedByManager },
                ClaimStatusType.Paid,
                ClaimReviewAction.MarkedAsPaid,
                null,
                $"Claim #{claimId} marked as paid!");
        }

        private async Task<ClaimWorkflowResult> MoveClaimAsync(
            int claimId,
            int reviewerUserId,
            string requiredRole,
            ClaimStatusType[] allowedFromStatuses,
            ClaimStatusType newStatus,
            ClaimReviewAction action,
            string? comments,
            string successMessage)
        {
            var claim = await _context.Claims.FirstOrDefaultAsync(c => c.claimId == claimId);
            if (claim == null)
                return ClaimWorkflowResult.Failed($"Claim #{claimId} could not be found.");

            var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.userId == reviewerUserId);
            if (reviewer == null || reviewer.userRole != requiredRole)
                return ClaimWorkflowResult.Failed("You are not allowed to perform this claim action.");

            var currentStatus = (ClaimStatusType)claim.statusId;
            if (!allowedFromStatuses.Contains(currentStatus))
                return ClaimWorkflowResult.Failed($"Claim #{claimId} is no longer in a valid state for this action.");

            var cleanComments = CleanComments(comments);
            if (cleanComments?.Length > 500)
                return ClaimWorkflowResult.Failed("Review comments cannot exceed 500 characters.");

            var previousStatusId = claim.statusId;
            claim.statusId = (int)newStatus;

            _context.ClaimReviews.Add(new ClaimReview
            {
                claimId = claim.claimId,
                reviewerUserId = reviewer.userId,
                reviewerRole = reviewer.userRole,
                action = action.ToString(),
                fromStatusId = previousStatusId,
                toStatusId = (int)newStatus,
                comments = cleanComments,
                reviewedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return ClaimWorkflowResult.Passed(successMessage);
        }

        private static string? CleanComments(string? comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                return null;

            return comments.Trim();
        }
    }
}
