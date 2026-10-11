using System.Net;
using System.Text.Json;
using Mcba.AdminPortal.Controllers;
using Mcba.AdminPortal.Models;
using Mcba.AdminPortal.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminPortal.Tests.Controllers;

public class PayeesControllerTests
{
    // --- Index ---

    [Theory]
    [InlineData(null, "/api/payees")]
    [InlineData("", "/api/payees")]
    [InlineData("   ", "/api/payees")]
    [InlineData("VIC", "/api/payees?state=VIC")]
    public async Task Index_RequestsPayeesWithOptionalStateFilter(string? state, string expectedUrl)
    {
        var api = new FakeAdminApi(body: new List<PayeeViewModel>());

        await Controller(api).Index(state!);

        Assert.Equal(HttpMethod.Get, api.Request.Method);
        Assert.Equal(expectedUrl, api.Request.PathAndQuery);
    }

    [Fact]
    public async Task Index_ShowsPayeesReturnedByApiAndRemembersFilter()
    {
        var api = new FakeAdminApi(body: new[] { NewPayee("Telstra"), NewPayee("Yarra Valley Water") });
        var controller = Controller(api);

        var result = await controller.Index("VIC");

        var view = Assert.IsType<ViewResult>(result);
        var payees = Assert.IsType<List<PayeeViewModel>>(view.Model);
        Assert.Equal(["Telstra", "Yarra Valley Water"], payees.Select(p => p.Name));
        Assert.Equal("VIC", controller.ViewData["State"]);
    }

    // --- Create ---

    [Fact]
    public void CreateGet_ReturnsEmptyForm()
    {
        var result = Controller(new FakeAdminApi()).Create();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task CreatePost_Valid_PostsPayeeToApiAndRedirects()
    {
        var api = new FakeAdminApi(HttpStatusCode.Created);
        var controller = Controller(api);
        var payee = NewPayee("Origin Energy");

        var result = await controller.Create(payee);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PayeesController.Index), redirect.ActionName);
        Assert.Equal("Payee Origin Energy was created.", controller.TempData["Message"]);

        Assert.Equal(HttpMethod.Post, api.Request.Method);
        Assert.Equal("/api/payees", api.Request.PathAndQuery);
        var sent = JsonSerializer.Deserialize<PayeeViewModel>(api.Request.Body!, JsonSerializerOptions.Web)!;
        Assert.Equal(payee.Name, sent.Name);
        Assert.Equal(payee.State, sent.State);
        Assert.Equal(payee.Phone, sent.Phone);
    }

    [Fact]
    public async Task CreatePost_InvalidModel_ReturnsViewWithoutCallingApi()
    {
        var api = new FakeAdminApi(HttpStatusCode.Created);
        var controller = Controller(api);
        controller.ModelState.AddModelError(nameof(PayeeViewModel.Postcode), "Postcode must be 4 digits.");
        var payee = NewPayee();

        var result = await controller.Create(payee);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(payee, view.Model);
        Assert.Empty(api.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CreatePost_ApiRejects_ReturnsViewWithError(HttpStatusCode status)
    {
        var controller = Controller(new FakeAdminApi(status));
        var payee = NewPayee();

        var result = await controller.Create(payee);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(payee, view.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Null(controller.TempData["Message"]);
    }

    private static PayeesController Controller(FakeAdminApi api) => new PayeesController(api).LoggedInAsAdmin();

    public static PayeeViewModel NewPayee(string name = "Origin Energy") => new()
    {
        Name = name,
        Address = "123 Collins Street",
        City = "Melbourne",
        State = "VIC",
        Postcode = "3000",
        Phone = "(03) 9999 8888"
    };
}
