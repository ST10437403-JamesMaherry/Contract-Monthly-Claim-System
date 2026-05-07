using Contract_Monthly_Claim_System.Filters;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [RoleAuthorization("Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly IDataService _dataService; // Service for data operations 
        private readonly IClaimWorkflowService _claimWorkflowService; // Handles claim status transitions
        private readonly IDocumentAccessService _documentAccessService; // Validates document downloads

        // Constructor: injects the data service via dependency injection
        public CoordinatorController(
            IDataService dataService,
            IClaimWorkflowService claimWorkflowService,
            IDocumentAccessService documentAccessService)
        {
            _dataService = dataService;
            _claimWorkflowService = claimWorkflowService;
            _documentAccessService = documentAccessService;
        }

        #region Claim Review Dashboard

        // Displays claims submitted by lecturers that are pending coordinator review
        public async Task<IActionResult> ReviewClaims()
        {
            ViewBag.UserRole = "Coordinator";
            ViewData["Title"] = "Review Claims";

            var claims = await _dataService.GetClaimsAsync();
            var pendingClaims = claims.Where(c => c.statusId == 1).ToList(); // Only show "Submitted" claims
            var approvedToday = claims.Count(c => c.statusId == 2 && c.submissionDate.Date == DateTime.Today);

            var users = await _dataService.GetUsersAsync();
            ViewBag.Users = users;

            var allDocuments = await _dataService.GetDocumentsAsync();
            ViewBag.Documents = allDocuments;

            // Pass summary stats to sidebar
            ViewBag.PendingClaims = pendingClaims.Count;
            ViewBag.ApprovedToday = approvedToday;

            return View(pendingClaims);
        }

        #endregion

        #region Claim Approval & Rejection

        // Approves a claim and updates its status to "Approved by Coordinator"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int claimId, string comments)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _claimWorkflowService.ApproveByCoordinatorAsync(claimId, currentUserId.Value, comments);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction("ReviewClaims");
        }

        // Rejects a claim and updates its status to "Rejected by Coordinator"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int claimId, string comments)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return RedirectToAction("Login", "Auth");

            var result = await _claimWorkflowService.RejectByCoordinatorAsync(claimId, currentUserId.Value, comments);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction("ReviewClaims");
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
