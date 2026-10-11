using Mcba.AdminApi.Controllers;
using Mcba.AdminApi.Repositories;
using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminApi.Tests.Controllers;

public class PayeesControllerTests : IDisposable
{
    private readonly McbaContext _context = TestDatabase.Create();
    private readonly PayeesController _controller;

    public PayeesControllerTests() => _controller = new PayeesController(new PayeeRepository(_context));

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData(null, 3)]
    [InlineData("VIC", 2)]
    [InlineData("QLD", 0)]
    public async Task GetAll_ReturnsOkWithMatchingPayees(string? state, int expectedCount)
    {
        var result = await _controller.GetAll(state);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payees = Assert.IsAssignableFrom<IEnumerable<Payee>>(ok.Value);
        Assert.Equal(expectedCount, payees.Count());
    }

    [Fact]
    public async Task Get_ExistingId_ReturnsOkWithPayee()
    {
        var id = _context.Payees.Single(p => p.Name == "Telstra").PayeeID;

        var result = await _controller.Get(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payee = Assert.IsType<Payee>(ok.Value);
        Assert.Equal(id, payee.PayeeID);
    }

    [Fact]
    public async Task Get_MissingId_ReturnsNotFound()
    {
        var result = await _controller.Get(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtGetWithNewPayee()
    {
        var result = await _controller.Create(TestDatabase.NewPayee("Origin Energy"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var payee = Assert.IsType<Payee>(created.Value);
        Assert.Equal(nameof(PayeesController.Get), created.ActionName);
        Assert.Equal(payee.PayeeID, created.RouteValues!["id"]);
        Assert.Equal(4, _context.Payees.Count());
    }
}
