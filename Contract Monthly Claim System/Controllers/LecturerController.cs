using Contract_Monthly_Claim_System.Filters;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [RoleAuthorization("Lecturer")]
    public class LecturerController : Controller
    {
        private readonly IDataService _dataService;
        private readonly IDocumentStorageService _documentStorageService;
        private readonly IDocumentAccessService _documentAccessService;

        public LecturerController(
            IDataService dataService,
            IDocumentStorageService documentStorageService,
            IDocumentAccessService documentAccessService)
        {
            _dataService = dataService;
            _documentStorageService = documentStorageService;
            _documentAccessService = documentAccessService;
        }

        #region Dashboard & Overview

        // Displays the lecturer dashboard for the logged-in user only
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.UserRole = "Lecturer";
            ViewData["Title"] = "Dashboard & My Claims";

            try
            {
                // Get the logged-in user's ID from session
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var claims = await _dataService.GetClaimsAsync();
                var users = await _dataService.GetUsersAsync();

                // Get only the current user's claims
                var userClaims = claims.Where(c => c.userId == currentUserId).ToList();
                var currentUser = users.FirstOrDefault(u => u.userId == currentUserId);

                // Pass data to view for display
                ViewBag.CurrentUser = currentUser;
                ViewBag.CurrentUserId = currentUserId;
                ViewBag.CurrentUserHourlyRate = currentUser?.hourlyRate ?? 0;
                ViewBag.TotalClaims = userClaims.Count;
                ViewBag.ApprovedClaims = userClaims.Count(c => c.statusId == 3 || c.statusId == 6);
                ViewBag.PendingClaims = userClaims.Count(c => c.statusId == 1 || c.statusId == 2);

                return View(userClaims);
            }
            catch (Exception)
            {
                ViewBag.TotalClaims = 0;
                ViewBag.ApprovedClaims = 0;
                ViewBag.PendingClaims = 0;
                return View(new List<Claim>());
            }
        }

        #endregion

        #region Claim Submission

        // Shows the claim submission form for the logged-in user
        public async Task<IActionResult> SubmitClaim()
        {
            ViewBag.UserRole = "Lecturer";
            ViewData["Title"] = "Submit Claim";

            // Get the logged-in user's ID
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Get the current user's information
            var users = await _dataService.GetUsersAsync();
            var currentUser = users.FirstOrDefault(u => u.userId == currentUserId);

            if (currentUser == null)
            {
                TempData["Error"] = "User not found. Please login again.";
                return RedirectToAction("Login", "Auth");
            }

            // Pass current user info to the view
            ViewBag.CurrentUser = currentUser;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.MaxDocuments = _documentStorageService.MaxFilesPerClaim;
            ViewBag.MaxUploadSizeMB = _documentStorageService.MaxFileSizeBytes / 1024 / 1024;

            // Create a claim with the user ID pre-set
            var claim = new Claim { userId = currentUserId.Value };
            return View(claim);
        }

        // Handles form submission for a new claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim claim, List<IFormFile> documents)
        {
            ViewBag.UserRole = "Lecturer";
            ViewData["Title"] = "Submit Claim";

            // Get the logged-in user's ID
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Remove properties that should be set by the server
            ModelState.Remove("hourlyRate");
            ModelState.Remove("claimId");
            ModelState.Remove("totalAmount");
            ModelState.Remove("submissionDate");
            ModelState.Remove("statusId");
            ModelState.Remove("User");
            ModelState.Remove("Status");
            ModelState.Remove("Documents");
            ModelState.Remove("ClaimReviews");

            // Force the claim to use the logged-in user's ID
            claim.userId = currentUserId.Value;

            var preparedDocuments = new List<DocumentUploadResult>();
            var uploadedDocuments = documents?.Where(d => d.Length > 0).ToList() ?? new List<IFormFile>();

            if (uploadedDocuments.Count > _documentStorageService.MaxFilesPerClaim)
            {
                ModelState.AddModelError("", $"You can upload a maximum of {_documentStorageService.MaxFilesPerClaim} supporting documents.");
            }

            foreach (var file in uploadedDocuments)
            {
                var uploadResult = await _documentStorageService.PrepareUploadAsync(file);
                if (uploadResult.Success)
                {
                    preparedDocuments.Add(uploadResult);
                }
                else
                {
                    ModelState.AddModelError("", uploadResult.ErrorMessage ?? $"'{file.FileName}' could not be uploaded.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the user's hourly rate
                    var allUsers = await _dataService.GetUsersAsync();
                    var user = allUsers.FirstOrDefault(u => u.userId == currentUserId);
                    if (user == null)
                    {
                        ModelState.AddModelError("", "User not found. Please login again.");
                        return View(claim);
                    }

                    var userHourlyRate = user.hourlyRate;

                    // Set claim metadata
                    claim.submissionDate = DateTime.Now;
                    claim.statusId = 1; // Submitted status
                    claim.hourlyRate = userHourlyRate;
                    claim.totalAmount = claim.hoursWorked * userHourlyRate;

                    // Clear navigation properties
                    claim.User = default!;
                    claim.Status = default!;
                    claim.Documents = new List<Document>();

                    // Save claim to database
                    await _dataService.AddClaimAsync(claim);

                    // Verify the claim ID was generated
                    if (claim.claimId == 0)
                    {
                        throw new Exception("Failed to generate claim ID.");
                    }

                    // Process validated supporting documents
                    foreach (var preparedDocument in preparedDocuments)
                    {
                        var document = new Document
                        {
                            claimId = claim.claimId,
                            fileName = preparedDocument.FileName,
                            uploadDate = DateTime.Now,
                            fileType = preparedDocument.FileExtension,
                            fileSize = preparedDocument.FileSize,
                            Claim = default!
                        };

                        await _dataService.AddDocumentAsync(document);

                        if (document.documentId == 0)
                        {
                            throw new Exception($"Failed to save document: {preparedDocument.FileName}");
                        }

                        await _documentStorageService.SaveDocumentFileAsync(document.documentId, preparedDocument.FileData);
                    }

                    TempData["Success"] = $"Claim submitted successfully! Your hourly rate of R {userHourlyRate:F2} was applied.";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error submitting claim: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    ModelState.AddModelError("", $"An error occurred while submitting your claim: {ex.Message}");
                }
            }
            else
            {
                // Log validation errors
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"Validation error: {error.ErrorMessage}");
                    }
                }
            }

            // If we get here, re-populate the current user info for the view
            var allUsers2 = await _dataService.GetUsersAsync();
            var currentUser = allUsers2.FirstOrDefault(u => u.userId == currentUserId);
            ViewBag.CurrentUser = currentUser;
            ViewBag.MaxDocuments = _documentStorageService.MaxFilesPerClaim;
            ViewBag.MaxUploadSizeMB = _documentStorageService.MaxFileSizeBytes / 1024 / 1024;

            return View(claim);
        }

        #endregion

        #region Document Handling 

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
