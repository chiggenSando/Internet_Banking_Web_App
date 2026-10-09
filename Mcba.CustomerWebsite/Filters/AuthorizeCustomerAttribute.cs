using Mcba.CustomerWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mcba.CustomerWebsite.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeCustomerAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var customerID = context.HttpContext.Session.GetInt32(nameof(Customer.CustomerID));
        if (!customerID.HasValue)
            context.Result = new RedirectToActionResult("Login", "Login", null);
    }
}
