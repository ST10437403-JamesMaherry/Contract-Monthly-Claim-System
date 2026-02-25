using System.ComponentModel.DataAnnotations;

namespace Contract_Monthly_Claim_System.Models
{
    public class Claim
    {
        public int claimId { get; set; }

        [Required]
        public int userId { get; set; }

        [Required]
        [Range(0.5, 180, ErrorMessage = "Hours worked must be between 0.5 and 180")]
        public decimal hoursWorked { get; set; }

        [Required]
        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        public decimal hourlyRate { get; set; }

        public decimal totalAmount { get; set; }

        [Required]
        public int statusId { get; set; }

        public DateTime submissionDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ClaimStatus Status { get; set; }
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
