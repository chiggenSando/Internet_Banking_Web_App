using Mcba.CustomerWebsite.Controllers;
using Mcba.CustomerWebsite.Tests.Common;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Mcba.CustomerWebsite.Tests;

public class LoginControllerTests : TestBase
{
    private readonly LoginController _controller;

    public LoginControllerTests()
    {
        _controller = WithSession(new LoginController(Context));
    }

    [Fact]
    public async Task Login_Post_CorrectDetails_LogsInAndRedirectsToAccounts()
    {
        // Act.
        var result = await _controller.Login(LoginID, Password);

        // Assert.
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Customer", redirect.ControllerName);
        Assert.Equal(CustomerID, _controller.HttpContext.Session.GetInt32(nameof(Customer.CustomerID)));
    }

    [Theory]
    [InlineData(LoginID, "wrongPassword")]
    [InlineData(LoginID, "")]
    [InlineData("99999999", Password)]
    public async Task Login_Post_WrongDetails_ShowsErrorAndDoesNotLogIn(string loginID, string password)
    {
        // Act.
        var result = await _controller.Login(loginID, password);

        // Assert.
        Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ContainsKey("LoginFailed"));
        Assert.Null(_controller.HttpContext.Session.GetInt32(nameof(Customer.CustomerID)));
    }

    [Fact]
    public async Task Login_Post_LockedLogin_IsRefused()
    {
        // Arrange.
        Context.Logins.Find(LoginID).IsLocked = true;
        Context.SaveChanges();

        // Act.
        var result = await _controller.Login(LoginID, Password);

        // Assert.
        Assert.IsType<ViewResult>(result);
        Assert.Null(_controller.HttpContext.Session.GetInt32(nameof(Customer.CustomerID)));
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        // Arrange.
        var controller = WithLoggedInCustomer(new LoginController(Context));

        // Act.
        var result = controller.Logout();

        // Assert.
        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32(nameof(Customer.CustomerID)));
    }
}