using Contract_Monthly_Claim_System.Filters;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [RoleAuthorization("Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly IDataService _dataService; // Service for data operations 

        // Constructor: injects the data service via dependency injection
        public CoordinatorController(IDataService dataService)
        {
            _dataService = dataService;
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
        public async Task<IActionResult> ApproveClaim(int claimId, string comments)
        {
            var claims = await _dataService.GetClaimsAsync();
            var claim = claims.FirstOrDefault(c => c.claimId == claimId);

            if (claim != null)
            {
                claim.statusId = 2; // Approved by Coordinator
                await _dataService.UpdateClaimAsync(claim);
                TempData["Success"] = $"Claim #{claimId} approved successfully!";
            }

            return RedirectToAction("ReviewClaims");
        }

        // Rejects a claim and updates its status to "Rejected by Coordinator"
        [HttpPost]
        public async Task<IActionResult> RejectClaim(int claimId, string comments)
        {
            var claims = await _dataService.GetClaimsAsync();
            var claim = claims.FirstOrDefault(c => c.claimId == claimId);

            if (claim != null)
            {
                claim.statusId = 4; // Rejected by Coordinator
                await _dataService.UpdateClaimAsync(claim);
                TempData["Success"] = $"Claim #{claimId} rejected!";
            }

            return RedirectToAction("ReviewClaims");
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
