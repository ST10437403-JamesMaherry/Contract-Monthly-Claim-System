using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Models
{
    public class Document
    {
        public int documentId { get; set; }

        [Required]
        public int claimId { get; set; }

        [Required]
        [StringLength(255)]
        public string fileName { get; set; }

        public DateTime uploadDate { get; set; }

        [StringLength(50)]
        public string fileType { get; set; }

        public long fileSize { get; set; }

        // Navigation property
        public virtual Claim Claim { get; set; }
    }
}
