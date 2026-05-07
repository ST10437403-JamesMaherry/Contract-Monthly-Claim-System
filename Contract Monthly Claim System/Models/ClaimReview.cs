using System.ComponentModel.DataAnnotations;

namespace Contract_Monthly_Claim_System.Models
{
    public class ClaimReview
    {
        public int claimReviewId { get; set; }

        [Required]
        public int claimId { get; set; }

        [Required]
        public int reviewerUserId { get; set; }

        [Required]
        [StringLength(50)]
        public string reviewerRole { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string action { get; set; } = string.Empty;

        [Required]
        public int fromStatusId { get; set; }

        [Required]
        public int toStatusId { get; set; }

        public DateTime reviewedAt { get; set; }

        [StringLength(500, ErrorMessage = "Review comments cannot exceed 500 characters")]
        public string? comments { get; set; }

        // Navigation properties
        public virtual Claim Claim { get; set; } = default!;
        public virtual User Reviewer { get; set; } = default!;
    }
}
