using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Models
{
    public class User
    {
        public int userId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string phoneNumber { get; set; }
        public string userRole { get; set; }
        public decimal hourlyRate { get; set; } // new hourly rate propert set by HR

        // Authentication Fields
        public string passwordHash { get; set; }
        public string passwordSalt { get; set; }

        // Navigation property
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
