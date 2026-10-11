using Mcba.AdminApi.Repositories;
using Mcba.Data;

namespace Mcba.AdminApi.Tests.Repositories;

public class PayeeRepositoryTests : IDisposable
{
    private readonly McbaContext _context = TestDatabase.Create();
    private readonly PayeeRepository _repo;

    public PayeeRepositoryTests() => _repo = new PayeeRepository(_context);

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllAsync_NoState_ReturnsAllPayees(string? state)
    {
        var payees = await _repo.GetAllAsync(state);

        Assert.Equal(3, payees.Count());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPayeesOrderedByName()
    {
        var payees = await _repo.GetAllAsync(null);

        Assert.Equal(["AGL Energy", "Telstra", "Yarra Valley Water"], payees.Select(p => p.Name));
    }

    [Theory]
    [InlineData("VIC", 2)]
    [InlineData("vic", 2)] // filter is case-insensitive
    [InlineData("NSW", 1)]
    [InlineData("QLD", 0)]
    public async Task GetAllAsync_State_ReturnsOnlyPayeesInThatState(string state, int expectedCount)
    {
        var payees = (await _repo.GetAllAsync(state)).ToList();

        Assert.Equal(expectedCount, payees.Count);
        Assert.All(payees, p => Assert.Equal(state.ToUpper(), p.State));
    }

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsPayee()
    {
        var expected = _context.Payees.Single(p => p.Name == "Telstra");

        var payee = await _repo.GetAsync(expected.PayeeID);

        Assert.NotNull(payee);
        Assert.Equal("Telstra", payee.Name);
    }

    [Fact]
    public async Task GetAsync_MissingId_ReturnsNull()
    {
        var payee = await _repo.GetAsync(999);

        Assert.Null(payee);
    }

    [Fact]
    public async Task AddAsync_SavesPayeeAndGeneratesId()
    {
        var payee = TestDatabase.NewPayee("Origin Energy");

        var created = await _repo.AddAsync(payee);

        Assert.NotEqual(0, created.PayeeID);
        Assert.Equal(4, _context.Payees.Count());
        Assert.Equal("Origin Energy", _context.Payees.Single(p => p.PayeeID == created.PayeeID).Name);
    }
}
