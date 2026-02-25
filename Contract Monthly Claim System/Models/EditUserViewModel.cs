namespace Contract_Monthly_Claim_System.Models
{
    public class EditUserViewModel
    {
        public int userId { get; set; }
        public string firstName { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string phoneNumber { get; set; } = string.Empty;
        public string userRole { get; set; } = string.Empty;
        public decimal hourlyRate { get; set; }
    }
}
