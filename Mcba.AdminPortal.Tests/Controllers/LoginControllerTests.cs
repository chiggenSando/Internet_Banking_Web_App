using Mcba.AdminPortal.Controllers;
using Mcba.AdminPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminPortal.Tests.Controllers;

public class LoginControllerTests
{
    private readonly LoginController _controller = new LoginController().WithSession();

    [Fact]
    public void Login_CorrectCredentials_StoresAdminInSessionAndRedirectsToDashboard()
    {
        var result = _controller.Login(new AdminLoginViewModel { Username = "admin", Password = "admin" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("admin", _controller.HttpContext.Session.GetString(LoginController.SessionKey));
    }

    [Theory]
    [InlineData("admin", "wrong")]
    [InlineData("wrong", "admin")]
    [InlineData("wrong", "wrong")]
    [InlineData("ADMIN", "admin")] // username is case-sensitive
    [InlineData("admin", "ADMIN")]
    public void Login_WrongCredentials_ReturnsViewWithErrorAndDoesNotLogIn(string username, string password)
    {
        var result = _controller.Login(new AdminLoginViewModel { Username = username, Password = password });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminLoginViewModel>(view.Model);
        Assert.Equal(username, model.Username);
        Assert.Null(model.Password); // the password is never sent back to the page
        Assert.False(_controller.ModelState.IsValid);
        Assert.Null(_controller.HttpContext.Session.GetString(LoginController.SessionKey));
    }

    [Fact]
    public void Login_InvalidModel_ReturnsViewWithoutLoggingIn()
    {
        // Model binding fills ModelState from the [Required] annotations before the action runs.
        _controller.ModelState.AddModelError(nameof(AdminLoginViewModel.Password), "The Password field is required.");
        var model = new AdminLoginViewModel { Username = "admin" };

        var result = _controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.Null(_controller.HttpContext.Session.GetString(LoginController.SessionKey));
    }

    [Fact]
    public void Logout_ClearsSessionAndRedirectsToLogin()
    {
        _controller.LoggedInAsAdmin();

        var result = _controller.Logout();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(LoginController.Login), redirect.ActionName);
        Assert.Empty(_controller.HttpContext.Session.Keys);
    }
}
