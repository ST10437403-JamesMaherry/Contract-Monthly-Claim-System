using Contract_Monthly_Claim_System.Filters;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [RoleAuthorization("HR")]
    public class HRController : Controller
    {
        private readonly IDataService _dataService;      // Service for data operations (users, claims)
        private readonly IPdfService _pdfService;        // Service for generating PDF reports
        private readonly IReportExportService _reportExportService; // Service for CSV reporting exports
        private readonly Services.IAuthenticationService _authService;  // Service for password management

        // Constructor: injects required services via dependency injection
        public HRController(IDataService dataService, IPdfService pdfService, IReportExportService reportExportService, Services.IAuthenticationService authService)
        {
            _dataService = dataService;
            _pdfService = pdfService;
            _reportExportService = reportExportService;
            _authService = authService;
        }

        #region Unified HR Dashboard

        // Displays the HR dashboard with user management and reporting capabilities
        public async Task<IActionResult> HRDashboard()
        {
            ViewBag.UserRole = "HR";
            ViewData["Title"] = "HR Dashboard";

            var users = await _dataService.GetUsersAsync();
            var claims = await _dataService.GetClaimsAsync();

            ViewBag.TotalClaims = claims?.Count ?? 0;
            ViewBag.ApprovedClaims = claims?.Count(c => c.statusId == 3 || c.statusId == 6) ?? 0;
            ViewBag.PendingClaims = claims?.Count(c => c.statusId == 1 || c.statusId == 2) ?? 0;
            ViewBag.TotalUsers = users?.Count ?? 0;
            ViewBag.PayableClaims = claims?
                .Where(c => c.statusId == (int)ClaimStatusType.ApprovedByManager || c.statusId == (int)ClaimStatusType.Paid)
                .OrderByDescending(c => c.submissionDate)
                .ToList() ?? new List<Claim>();
            ViewBag.CurrentBatchYear = DateTime.Now.Year;
            ViewBag.CurrentBatchMonth = DateTime.Now.Month;

            return View(users); // Renders Views/HR/HRDashboard.cshtml
        }

        // Alias for HRDashboard (for backward compatibility with navigation)
        public async Task<IActionResult> ManageUsers()
        {
            return await HRDashboard();
        }

        #endregion

        #region User Management

        // Shows the form to add a new system user
        public IActionResult AddUser()
        {
            ViewBag.UserRole = "HR";
            ViewData["Title"] = "Add New User";
            return View();
        }

        // Handles submission of a new user with password setup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(User user, string password, string confirmPassword)
        {
            ViewBag.UserRole = "HR";
            ViewData["Title"] = "Add New User";

            // Remove password fields from model validation
            ModelState.Remove("password");
            ModelState.Remove("confirmPassword");
            ModelState.Remove("passwordHash");
            ModelState.Remove("passwordSalt");

            // Validate passwords
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Password is required for new users.");
                return View(user);
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters long.");
                return View(user);
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(user);
            }

            // Check if email already exists
            var existingUsers = await _dataService.GetUsersAsync();
            if (existingUsers.Any(u => u.email.Equals(user.email, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("email", "A user with this email already exists.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Hash the password using authentication service
                    user.passwordHash = _authService.HashPassword(password, out string salt);
                    user.passwordSalt = salt;
                    user.mustChangePassword = true;

                    // Add user to database
                    await _dataService.AddUserAsync(user);

                    TempData["Success"] = $"User {user.firstName} {user.lastName} added successfully with a temporary password. They must change it on first login.";
                    return RedirectToAction("HRDashboard"); // Return to HR dashboard
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred while adding the user: {ex.Message}");
                }
            }

            return View(user); // Redisplay form with validation errors
        }

        // GET
        public async Task<IActionResult> EditUser(int id)
        {
            ViewBag.UserRole = "HR";
            ViewData["Title"] = "Edit User";

            var user = (await _dataService.GetUsersAsync()).FirstOrDefault(u => u.userId == id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("HRDashboard");
            }

            var model = new EditUserViewModel
            {
                userId = user.userId,
                firstName = user.firstName,
                lastName = user.lastName,
                email = user.email,
                phoneNumber = user.phoneNumber,
                userRole = user.userRole,
                hourlyRate = user.hourlyRate
            };

            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            ViewBag.UserRole = "HR";
            ViewData["Title"] = "Edit User";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = (await _dataService.GetUsersAsync())
                .FirstOrDefault(u => u.userId == model.userId);

            if (existingUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("HRDashboard");
            }

            // Only update allowed fields
            existingUser.firstName = model.firstName;
            existingUser.lastName = model.lastName;
            existingUser.email = model.email;
            existingUser.phoneNumber = model.phoneNumber;
            existingUser.userRole = model.userRole;
            existingUser.hourlyRate = model.hourlyRate;

            try
            {
                await _dataService.UpdateUserAsync(existingUser);
                TempData["Success"] = $"User {existingUser.firstName} {existingUser.lastName} updated successfully!";
                return RedirectToAction("HRDashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        #endregion

        #region Reports

        // Generates and downloads a PDF report of all approved/paid claims
        public async Task<IActionResult> DownloadPaymentReportPdf()
        {
            var claims = await _dataService.GetClaimsAsync();
            var users = await _dataService.GetUsersAsync();

            var approvedClaims = claims?.Where(c => c.statusId == (int)ClaimStatusType.ApprovedByManager || c.statusId == (int)ClaimStatusType.Paid).ToList() ?? new List<Claim>();
            var pdfBytes = _pdfService.GeneratePaymentReport(approvedClaims, users);

            return File(pdfBytes, "application/pdf", $"Payment-Report-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Generates and downloads a CSV report of all approved/paid claims
        public async Task<IActionResult> DownloadPaymentReportCsv()
        {
            var claims = await _dataService.GetClaimsAsync();
            var users = await _dataService.GetUsersAsync();
            var csvBytes = _reportExportService.GeneratePaymentReportCsv(claims ?? new List<Claim>(), users);

            return File(csvBytes, "text/csv", $"Payment-Report-{DateTime.Now:yyyyMMdd}.csv");
        }

        // Generates and downloads a PDF user directory report (optionally filtered by role)
        public async Task<IActionResult> DownloadUserReportPdf(string? role = null)
        {
            var users = await _dataService.GetUsersAsync();
            var pdfBytes = _pdfService.GenerateUserReport(users, role ?? string.Empty);

            var fileName = string.IsNullOrEmpty(role)
                ? $"User-Directory-Report-{DateTime.Now:yyyyMMdd}.pdf"
                : $"{role}-Users-Report-{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        // Generates and downloads a CSV user directory report (optionally filtered by role)
        public async Task<IActionResult> DownloadUserReportCsv(string? role = null)
        {
            var users = await _dataService.GetUsersAsync();
            var csvBytes = _reportExportService.GenerateUserReportCsv(users, role);

            var fileName = string.IsNullOrEmpty(role)
                ? $"User-Directory-Report-{DateTime.Now:yyyyMMdd}.csv"
                : $"{role}-Users-Report-{DateTime.Now:yyyyMMdd}.csv";

            return File(csvBytes, "text/csv", fileName);
        }

        // Generates and downloads a single claim invoice PDF
        public async Task<IActionResult> DownloadInvoicePdf(int claimId)
        {
            var claim = await _dataService.GetClaimAsync(claimId);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("HRDashboard");
            }

            var user = claim.User;
            if (user == null)
            {
                var users = await _dataService.GetUsersAsync();
                user = users.FirstOrDefault(u => u.userId == claim.userId);
            }

            if (user == null)
            {
                TempData["Error"] = "Unable to generate invoice because the lecturer could not be found.";
                return RedirectToAction("HRDashboard");
            }

            var pdfBytes = _pdfService.GenerateInvoice(claim, user);
            return File(pdfBytes, "application/pdf", $"Invoice-Claim-{claim.claimId}-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Generates and downloads a monthly payment batch for payroll processing
        public async Task<IActionResult> DownloadMonthlyPaymentBatchCsv(int? year = null, int? month = null)
        {
            var batchYear = year ?? DateTime.Now.Year;
            var batchMonth = month ?? DateTime.Now.Month;

            if (batchMonth < 1 || batchMonth > 12)
                return BadRequest("Month must be between 1 and 12.");

            var claims = await _dataService.GetClaimsAsync();
            var users = await _dataService.GetUsersAsync();
            var csvBytes = _reportExportService.GenerateMonthlyPaymentBatchCsv(claims ?? new List<Claim>(), users, batchYear, batchMonth);

            return File(csvBytes, "text/csv", $"Payment-Batch-{batchYear:D4}-{batchMonth:D2}.csv");
        }

        #endregion
    }
}
