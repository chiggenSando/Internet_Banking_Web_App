using Mcba.Data;
using Mcba.Data.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Mcba.AdminApi.Repositories;

/// <summary>
/// Entity Framework implementation of <see cref="ILoginsRepository"/>.
/// </summary>
public class LoginsRepository(McbaContext _context) : ILoginsRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<LoginDto>> LoginsAsync()
    {
        return await _context.Logins
            .OrderBy(l => l.LoginID)
            .Select(l => new LoginDto
            {
                LoginID = l.LoginID,
                CustomerID = l.CustomerID,
                CustomerName = l.Customer.Name,
                IsLocked = l.IsLocked
            }).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> SetLockedAsync(string loginId, bool locked)
    {
        var login = await _context.Logins.FindAsync(loginId);
        if (login is null)
        {
            return false;
        }
        login.IsLocked = locked;
        await _context.SaveChangesAsync();
        return true;
    }
}
