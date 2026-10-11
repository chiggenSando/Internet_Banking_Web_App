using Mcba.AdminApi.Repositories;
using Mcba.Data;

namespace Mcba.AdminApi.Tests.Repositories;

public class LoginsRepositoryTests : IDisposable
{
    private readonly McbaContext _context = TestDatabase.Create();
    private readonly LoginsRepository _repo;

    public LoginsRepositoryTests() => _repo = new LoginsRepository(_context);

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task LoginsAsync_ReturnsAllLoginsOrderedById()
    {
        var logins = await _repo.LoginsAsync();

        Assert.Equal([TestDatabase.UnlockedLoginID, TestDatabase.LockedLoginID], logins.Select(l => l.LoginID));
    }

    [Fact]
    public async Task LoginsAsync_IncludesCustomerNameAndLockStatus()
    {
        var login = (await _repo.LoginsAsync()).Single(l => l.LoginID == TestDatabase.LockedLoginID);

        Assert.Equal(2200, login.CustomerID);
        Assert.Equal("Rodney Cocker", login.CustomerName);
        Assert.True(login.IsLocked);
    }

    // Covers locking, unlocking, and setting a login to the state it is already in.
    [Theory]
    [InlineData(TestDatabase.UnlockedLoginID, true)]
    [InlineData(TestDatabase.LockedLoginID, false)]
    [InlineData(TestDatabase.UnlockedLoginID, false)]
    [InlineData(TestDatabase.LockedLoginID, true)]
    public async Task SetLockedAsync_ExistingLogin_UpdatesAndReturnsTrue(string loginId, bool locked)
    {
        var found = await _repo.SetLockedAsync(loginId, locked);

        Assert.True(found);
        _context.ChangeTracker.Clear();
        Assert.Equal(locked, (await _context.Logins.FindAsync(loginId))!.IsLocked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetLockedAsync_MissingLogin_ReturnsFalse(bool locked)
    {
        var found = await _repo.SetLockedAsync(TestDatabase.MissingLoginID, locked);

        Assert.False(found);
    }

    [Fact]
    public async Task SetLockedAsync_OnlyChangesTheGivenLogin()
    {
        await _repo.SetLockedAsync(TestDatabase.UnlockedLoginID, true);

        _context.ChangeTracker.Clear();
        Assert.True((await _context.Logins.FindAsync(TestDatabase.LockedLoginID))!.IsLocked);
        Assert.Equal(2, _context.Logins.Count(l => l.IsLocked));
    }
}
