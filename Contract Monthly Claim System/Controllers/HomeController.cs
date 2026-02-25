using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Get the user's role from session
            var userRole = HttpContext.Session.GetString("UserRole");

            // Redirect to appropriate dashboard based on role
            return userRole switch
            {
                "Lecturer" => RedirectToAction("Dashboard", "Lecturer"),
                "Coordinator" => RedirectToAction("ReviewClaims", "Coordinator"),
                "Manager" => RedirectToAction("ApproveClaims", "Manager"),
                "HR" => RedirectToAction("ManageUsers", "HR"),
                _ => RedirectToAction("Login", "Auth") // If no role, redirect to login
            };
        }
    }
}
