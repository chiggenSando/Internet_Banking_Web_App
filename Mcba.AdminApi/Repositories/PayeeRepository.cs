using Mcba.Data;
using Mcba.Data.Models;
using Microsoft.EntityFrameworkCore;
namespace Mcba.AdminApi.Repositories;

public class PayeeRepository(McbaContext _context) : IPayeeRepository
{
    public async Task<IEnumerable<Payee>> GetAllAsync(string? state)
    {
        var payees = _context.Payees.AsQueryable();
        if (!string.IsNullOrWhiteSpace(state))
            payees = payees.Where(p => p.State == state.ToUpper());
        return await payees.OrderBy(p => p.Name).ToListAsync();
    }
    public async Task<Payee?> GetAsync(int id)
    {
        return await _context.Payees.FindAsync(id);
    }
    public async Task<Payee> AddAsync(Payee payee)
    {
        _context.Payees.Add(payee);
        await _context.SaveChangesAsync();

        return payee;
    }
}
