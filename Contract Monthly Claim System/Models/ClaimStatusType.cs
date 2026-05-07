namespace Contract_Monthly_Claim_System.Models
{
    public enum ClaimStatusType
    {
        Submitted = 1,
        ApprovedByCoordinator = 2,
        ApprovedByManager = 3,
        RejectedByCoordinator = 4,
        RejectedByManager = 5,
        Paid = 6
    }
}
