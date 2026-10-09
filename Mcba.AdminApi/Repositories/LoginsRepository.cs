using Mcba.Data;
using Mcba.Data.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Mcba.AdminApi.Repositories;

public class LoginsRepository(McbaContext _context) : ILoginsRepository
{
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
