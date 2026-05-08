using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthenticationService _authService;

        public AuthController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        // GET: Login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter both email and password.";
                return View();
            }

            var user = await _authService.ValidateUserAsync(email, password);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // Start each successful login with a clean session.
            HttpContext.Session.Clear();

            // Set session variables
            HttpContext.Session.SetInt32("UserId", user.userId);
            HttpContext.Session.SetString("UserRole", user.userRole);
            HttpContext.Session.SetString("UserName", $"{user.firstName} {user.lastName}");
            HttpContext.Session.SetString("UserEmail", user.email);
            HttpContext.Session.SetString("MustChangePassword", user.mustChangePassword ? "true" : "false");

            if (user.mustChangePassword)
            {
                TempData["Info"] = "Please change your temporary password before continuing.";
                return RedirectToAction("ChangePassword");
            }

            // Redirect to role-specific dashboard
            return RedirectToDashboard();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login");

            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.MustChangePassword = HttpContext.Session.GetString("MustChangePassword") == "true";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.MustChangePassword = HttpContext.Session.GetString("MustChangePassword") == "true";

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Please complete all password fields.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "New password must be at least 6 characters long.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                return View();
            }

            if (newPassword == currentPassword)
            {
                ViewBag.Error = "New password must be different from the temporary password.";
                return View();
            }

            var passwordChanged = await _authService.ChangePasswordAsync(userId.Value, currentPassword, newPassword);
            if (!passwordChanged)
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            HttpContext.Session.SetString("MustChangePassword", "false");
            TempData["Success"] = "Your password has been updated.";

            return RedirectToDashboard();
        }

        // Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Access Denied page
        public IActionResult AccessDenied()
        {
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View();
        }

        // Helper method to redirect based on role
        private IActionResult RedirectToDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");

            return role switch
            {
                "Lecturer" => RedirectToAction("Dashboard", "Lecturer"),
                "Coordinator" => RedirectToAction("ReviewClaims", "Coordinator"),
                "Manager" => RedirectToAction("ApproveClaims", "Manager"),
                "HR" => RedirectToAction("HRDashboard", "HR"),
                _ => RedirectToAction("Login")
            };
        }
    }
}
