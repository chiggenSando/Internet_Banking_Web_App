using Mcba.AdminPortal.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mcba.AdminPortal.Filters;

// Redirects to the login page unless an admin is logged in.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var username = context.HttpContext.Session.GetString(LoginController.SessionKey);
        if (username is null)
            context.Result = new RedirectToActionResult("Login", "Login", null);
    }
}
