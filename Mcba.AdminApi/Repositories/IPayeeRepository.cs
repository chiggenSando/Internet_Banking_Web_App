using Mcba.Data.Models;

namespace Mcba.AdminApi.Repositories;

public interface IPayeeRepository
{
    public Task<IEnumerable<Payee>> GetAllAsync(string? state);
    public Task<Payee?> GetAsync(int id);
    public Task<Payee> AddAsync(Payee payee);
}
