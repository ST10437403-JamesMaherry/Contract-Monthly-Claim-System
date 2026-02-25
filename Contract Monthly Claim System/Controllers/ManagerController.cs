using Contract_Monthly_Claim_System.Filters;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [RoleAuthorization("Manager")]
    public class ManagerController : Controller
    {
        private readonly IDataService _dataService; // Service for data operations 

        // Constructor: injects the data service via dependency injection
        public ManagerController(IDataService dataService)
        {
            _dataService = dataService;
        }

        #region Approval Dashboard

        // Displays claims for final review with filtering options
        public async Task<IActionResult> ApproveClaims(string filter = "pending")
        {
            ViewBag.UserRole = "Manager";
            ViewData["Title"] = "Final Approval";
            ViewBag.CurrentFilter = filter;

            var claims = await _dataService.GetClaimsAsync();
            var allClaims = claims.ToList();

            // Filter claims based on the selected filter
            List<Contract_Monthly_Claim_System.Models.Claim> filteredClaims = filter switch
            {
                "all" => allClaims,
                "past" => allClaims.Where(c => c.statusId == 3 || c.statusId == 5 || c.statusId == 6).ToList(), // Manager processed claims
                "approved" => allClaims.Where(c => c.statusId == 3).ToList(), // Manager-approved (ready for payment)
                "paid" => allClaims.Where(c => c.statusId == 6).ToList(), // Paid claims
                _ => allClaims.Where(c => c.statusId == 2 || c.statusId == 4).ToList() // Default: pending manager review (coordinator verified)
            };

            var totalApprovedAmount = allClaims.Where(c => c.statusId == 3 || c.statusId == 6).Sum(c => c.totalAmount);

            var users = await _dataService.GetUsersAsync();
            ViewBag.Users = users;

            var allDocuments = await _dataService.GetDocumentsAsync();
            ViewBag.Documents = allDocuments;

            // Set sidebar stats
            ViewBag.PendingClaims = allClaims.Count(c => c.statusId == 2 || c.statusId == 4); // Coordinator-approved or Coordinator-rejected
            ViewBag.BudgetUsed = totalApprovedAmount;
            ViewBag.BudgetTotal = 50000;

            return View(filteredClaims);
        }

        #endregion

        #region Claim Approval & Rejection

        // Grants final approval to a claim 
        [HttpPost]
        public async Task<IActionResult> FinalApprove(int claimId)
        {
            var claims = await _dataService.GetClaimsAsync();
            var claim = claims.FirstOrDefault(c => c.claimId == claimId);

            if (claim != null)
            {
                claim.statusId = 3; // Approved by Manager
                await _dataService.UpdateClaimAsync(claim);
                TempData["Success"] = $"Claim #{claimId} approved! Ready for payment.";
            }

            return RedirectToAction("ApproveClaims");
        }

        // Rejects a claim at the manager level
        [HttpPost]
        public async Task<IActionResult> RejectClaim(int claimId)
        {
            var claims = await _dataService.GetClaimsAsync();
            var claim = claims.FirstOrDefault(c => c.claimId == claimId);

            if (claim != null)
            {
                claim.statusId = 5; // Rejected by Manager
                await _dataService.UpdateClaimAsync(claim);
                TempData["Success"] = $"Claim #{claimId} rejected!";
            }

            return RedirectToAction("ApproveClaims");
        }

        #endregion

        #region Payment Processing

        // Marks an approved claim as paid 
        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int claimId)
        {
            var claims = await _dataService.GetClaimsAsync();
            var claim = claims.FirstOrDefault(c => c.claimId == claimId);

            if (claim != null)
            {
                claim.statusId = 6; // Paid
                await _dataService.UpdateClaimAsync(claim);
                TempData["Success"] = $"Claim #{claimId} marked as paid!";
            }

            return RedirectToAction("ApproveClaims");
        }

        #endregion

        #region Document Handling

        // Serves a stored document file for download
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var document = (await _dataService.GetDocumentsAsync()).FirstOrDefault(d => d.documentId == documentId);
            if (document == null) return NotFound();

            var fileData = await _dataService.GetDocumentFileAsync(documentId);
            if (fileData == null) return NotFound();

            return File(fileData, "application/octet-stream", document.fileName);
        }

        #endregion
    }
}
