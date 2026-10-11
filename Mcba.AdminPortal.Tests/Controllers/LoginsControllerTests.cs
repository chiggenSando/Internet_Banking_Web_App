using System.Net;
using Mcba.AdminPortal.Controllers;
using Mcba.AdminPortal.Models;
using Mcba.AdminPortal.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminPortal.Tests.Controllers;

public class LoginsControllerTests
{
    [Fact]
    public async Task Index_ShowsLoginsReturnedByApi()
    {
        var api = new FakeAdminApi(body: new[]
        {
            new LoginViewModel { LoginID = "12345678", CustomerID = 2100, CustomerName = "Matthew Bolger" },
            new LoginViewModel { LoginID = "38074569", CustomerID = 2200, CustomerName = "Rodney Cocker", IsLocked = true }
        });

        var result = await Controller(api).Index();

        Assert.Equal(HttpMethod.Get, api.Request.Method);
        Assert.Equal("/api/logins", api.Request.PathAndQuery);

        var view = Assert.IsType<ViewResult>(result);
        var logins = Assert.IsType<List<LoginViewModel>>(view.Model);
        Assert.Equal(2, logins.Count);
        Assert.Equal("Rodney Cocker", logins[1].CustomerName);
        Assert.True(logins[1].IsLocked);
    }

    [Theory]
    [InlineData(true, "/api/logins/12345678/lock", "Login 12345678 was locked.")]
    [InlineData(false, "/api/logins/12345678/unlock", "Login 12345678 was unlocked.")]
    public async Task LockOrUnlock_Success_PutsToApiAndRedirectsWithMessage(bool locking, string expectedUrl, string expectedMessage)
    {
        var api = new FakeAdminApi(HttpStatusCode.NoContent);
        var controller = Controller(api);

        var result = locking ? await controller.Lock("12345678") : await controller.Unlock("12345678");

        Assert.Equal(HttpMethod.Put, api.Request.Method);
        Assert.Equal(expectedUrl, api.Request.PathAndQuery);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(LoginsController.Index), redirect.ActionName);
        Assert.Equal(expectedMessage, controller.TempData["Message"]);
    }

    [Theory]
    [InlineData(true, "Login 99999999 could not be locked.")]
    [InlineData(false, "Login 99999999 could not be unlocked.")]
    public async Task LockOrUnlock_ApiReturnsNotFound_RedirectsWithErrorMessage(bool locking, string expectedMessage)
    {
        var controller = Controller(new FakeAdminApi(HttpStatusCode.NotFound));

        var result = locking ? await controller.Lock("99999999") : await controller.Unlock("99999999");

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(expectedMessage, controller.TempData["Message"]);
    }

    [Fact]
    public async Task Lock_EscapesLoginIdInUrl()
    {
        var api = new FakeAdminApi(HttpStatusCode.NotFound);

        await Controller(api).Lock("../payees");

        Assert.Equal("/api/logins/..%2Fpayees/lock", api.Request.PathAndQuery);
    }

    private static LoginsController Controller(FakeAdminApi api) => new LoginsController(api).LoggedInAsAdmin();
}
