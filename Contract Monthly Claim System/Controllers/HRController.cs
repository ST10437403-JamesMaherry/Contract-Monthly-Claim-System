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
        private readonly Services.IAuthenticationService _authService;  // Service for password management

        // Constructor: injects required services via dependency injection
        public HRController(IDataService dataService, IPdfService pdfService, Services.IAuthenticationService authService)
        {
            _dataService = dataService;
            _pdfService = pdfService;
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

                    // Add user to database
                    await _dataService.AddUserAsync(user);

                    TempData["Success"] = $"User {user.firstName} {user.lastName} added successfully with password set!";
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

            var approvedClaims = claims?.Where(c => c.statusId == 3 || c.statusId == 6).ToList() ?? new List<Claim>();
            var pdfBytes = _pdfService.GeneratePaymentReport(approvedClaims, users);

            return File(pdfBytes, "application/pdf", $"Payment-Report-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Generates and downloads a PDF user directory report (optionally filtered by role)
        public async Task<IActionResult> DownloadUserReportPdf(string role = null)
        {
            var users = await _dataService.GetUsersAsync();
            var pdfBytes = _pdfService.GenerateUserReport(users, role);

            var fileName = string.IsNullOrEmpty(role)
                ? $"User-Directory-Report-{DateTime.Now:yyyyMMdd}.pdf"
                : $"{role}-Users-Report-{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        #endregion
    }
}
