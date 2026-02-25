using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Models
{
    public class ClaimStatus
    {
        public int statusId { get; set; }
        public string statusName { get; set; }

        // Navigation property
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
