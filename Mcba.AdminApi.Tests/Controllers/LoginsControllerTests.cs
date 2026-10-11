using Mcba.AdminApi.Controllers;
using Mcba.AdminApi.Repositories;
using Mcba.Data;
using Mcba.Data.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminApi.Tests.Controllers;

public class LoginsControllerTests : IDisposable
{
    private readonly McbaContext _context = TestDatabase.Create();
    private readonly LoginsController _controller;

    public LoginsControllerTests() => _controller = new LoginsController(new LoginsRepository(_context));

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetAll_ReturnsOkWithAllLogins()
    {
        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var logins = Assert.IsAssignableFrom<IEnumerable<LoginDto>>(ok.Value);
        Assert.Equal(2, logins.Count());
    }

    [Fact]
    public async Task Lock_ExistingLogin_ReturnsNoContentAndLocks()
    {
        var result = await _controller.Lock(TestDatabase.UnlockedLoginID);

        Assert.IsType<NoContentResult>(result);
        Assert.True(await IsLockedAsync(TestDatabase.UnlockedLoginID));
    }

    [Fact]
    public async Task Unlock_ExistingLogin_ReturnsNoContentAndUnlocks()
    {
        var result = await _controller.Unlock(TestDatabase.LockedLoginID);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await IsLockedAsync(TestDatabase.LockedLoginID));
    }

    [Fact]
    public async Task Lock_MissingLogin_ReturnsNotFound()
    {
        var result = await _controller.Lock(TestDatabase.MissingLoginID);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Unlock_MissingLogin_ReturnsNotFound()
    {
        var result = await _controller.Unlock(TestDatabase.MissingLoginID);

        Assert.IsType<NotFoundResult>(result);
    }

    private async Task<bool> IsLockedAsync(string loginId)
    {
        _context.ChangeTracker.Clear();
        return (await _context.Logins.FindAsync(loginId))!.IsLocked;
    }
}
