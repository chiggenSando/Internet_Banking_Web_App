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
                CustomerName = l.Customer.Name
            }).ToListAsync();
    }

    public async Task<bool> SetLockedAsync(string loginId, bool locked)
    {
        throw new NotImplementedException();
    }
}
