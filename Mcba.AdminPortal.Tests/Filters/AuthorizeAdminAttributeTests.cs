using System.Reflection;
using Mcba.AdminPortal.Controllers;
using Mcba.AdminPortal.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Mcba.AdminPortal.Tests.Filters;

public class AuthorizeAdminAttributeTests
{
    [Fact]
    public void NotLoggedIn_RedirectsToLogin()
    {
        var context = CreateContext(loggedIn: false);

        new AuthorizeAdminAttribute().OnAuthorization(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("Login", redirect.ControllerName);
        Assert.Equal("Login", redirect.ActionName);
    }

    [Fact]
    public void LoggedIn_AllowsRequestThrough()
    {
        var context = CreateContext(loggedIn: true);

        new AuthorizeAdminAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    // The filter only protects pages it is applied to, so check every admin page has it.
    [Theory]
    [InlineData(typeof(PayeesController))]
    [InlineData(typeof(LoginsController))]
    public void AdminControllers_RequireAdminLogin(Type controller)
    {
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAdminAttribute>());
    }

    [Fact]
    public void Dashboard_RequiresAdminLogin()
    {
        var index = typeof(HomeController).GetMethod(nameof(HomeController.Index))!;

        Assert.NotNull(index.GetCustomAttribute<AuthorizeAdminAttribute>());
    }

    [Fact]
    public void LoginPage_DoesNotRequireAdminLogin()
    {
        Assert.Null(typeof(LoginController).GetCustomAttribute<AuthorizeAdminAttribute>());
    }

    private static AuthorizationFilterContext CreateContext(bool loggedIn)
    {
        var httpContext = new DefaultHttpContext { Session = new ControllerTestHelper.TestSession() };
        if (loggedIn)
            httpContext.Session.SetString(LoginController.SessionKey, "admin");

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
