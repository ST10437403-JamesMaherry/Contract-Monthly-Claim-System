using Contract_Monthly_Claim_System.Filters;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [RoleAuthorization("Manager")]
    public class ManagerController : Controller
    {
        private readonly IDataService _dataService; // Service for data operations 
        private readonly IClaimWorkflowService _claimWorkflowService; // Handles claim status transitions
        private readonly IDocumentAccessService _documentAccessService; // Validates document downloads

        // Constructor: injects the data service via dependency injection
        public ManagerController(
            IDataService dataService,
            IClaimWorkflowService claimWorkflowService,
            IDocumentAccessService documentAccessService)
        {
            _dataService = dataService;
            _claimWorkflowService = claimWorkflowService;
            _documentAccessService = documentAccessService;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalApprove(int claimId, string comments)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _claimWorkflowService.ApproveByManagerAsync(claimId, currentUserId.Value, comments);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction("ApproveClaims");
        }

        // Rejects a claim at the manager level
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int claimId, string comments)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _claimWorkflowService.RejectByManagerAsync(claimId, currentUserId.Value, comments);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction("ApproveClaims");
        }

        #endregion

        #region Payment Processing

        // Marks an approved claim as paid 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int claimId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _claimWorkflowService.MarkAsPaidAsync(claimId, currentUserId.Value);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction("ApproveClaims");
        }

        #endregion

        #region Document Handling

        // Serves a stored document file for download
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var currentUserRole = HttpContext.Session.GetString("UserRole");
            if (currentUserId == null || string.IsNullOrEmpty(currentUserRole))
                return RedirectToAction("Login", "Auth");

            var download = await _documentAccessService.GetAuthorizedDownloadAsync(documentId, currentUserId.Value, currentUserRole);
            if (!download.Found) return NotFound();
            if (!download.Authorized) return RedirectToAction("AccessDenied", "Auth");

            return File(download.FileData!, download.ContentType, download.FileName);
        }

        #endregion
    }
}
