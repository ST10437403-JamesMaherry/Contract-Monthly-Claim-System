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

        public LecturerController(IDataService dataService)
        {
            _dataService = dataService;
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

            // Force the claim to use the logged-in user's ID
            claim.userId = currentUserId.Value;

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

                    // Process uploaded documents
                    if (documents != null && documents.Any(d => d.Length > 0))
                    {
                        foreach (var file in documents.Where(d => d.Length > 0))
                        {
                            if (file.Length <= 10 * 1024 * 1024)
                            {
                                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                                if (allowedExtensions.Contains(fileExtension))
                                {
                                    var document = new Document
                                    {
                                        claimId = claim.claimId,
                                        fileName = file.FileName,
                                        uploadDate = DateTime.Now,
                                        fileType = fileExtension,
                                        fileSize = file.Length,
                                        Claim = default!
                                    };

                                    await _dataService.AddDocumentAsync(document);

                                    if (document.documentId == 0)
                                    {
                                        throw new Exception($"Failed to save document: {file.FileName}");
                                    }

                                    using var memoryStream = new MemoryStream();
                                    await file.CopyToAsync(memoryStream);
                                    await _dataService.SaveDocumentFileAsync(document.documentId, memoryStream.ToArray());
                                }
                                else
                                {
                                    TempData["Warning"] = $"File '{file.FileName}' was skipped (invalid format).";
                                }
                            }
                            else
                            {
                                TempData["Warning"] = $"File '{file.FileName}' was skipped (too large).";
                            }
                        }
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

            return View(claim);
        }

        #endregion

        #region Document Handling 

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
