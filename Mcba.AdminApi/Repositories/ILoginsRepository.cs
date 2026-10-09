using Mcba.Data.Models;

namespace Mcba.AdminApi.Repositories;

public interface ILoginsRepository
{
    public Task<IEnumerable<Login>> LoginsAsync();
    public Task<bool> SetLockedAsync(string loginId, bool locked);
}
