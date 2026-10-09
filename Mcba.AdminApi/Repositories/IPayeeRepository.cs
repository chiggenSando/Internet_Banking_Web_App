using Mcba.Data.Models;
namespace Mcba.AdminApi.Repositories;

public interface IPayeeRepository
{
    Task<IEnumerable<Payee>> GetAllAsync(string? state);
    Task<Payee?> GetAsync(int id);
    Task<Payee> AddAsync(Payee payee);
}
