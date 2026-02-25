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

            // Set session variables
            HttpContext.Session.SetInt32("UserId", user.userId);
            HttpContext.Session.SetString("UserRole", user.userRole);
            HttpContext.Session.SetString("UserName", $"{user.firstName} {user.lastName}");
            HttpContext.Session.SetString("UserEmail", user.email);

            // Redirect to role-specific dashboard
            return RedirectToDashboard();
        }

        // Logout
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
