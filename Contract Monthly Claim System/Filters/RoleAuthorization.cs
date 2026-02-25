using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Contract_Monthly_Claim_System.Filters
{
    public class RoleAuthorization : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorization(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Check if user is logged in
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var userRole = context.HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                // Not logged in - redirect to login
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Check if user's role is allowed
            if (!_allowedRoles.Contains(userRole))
            {
                // Not authorized - redirect to access denied
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
