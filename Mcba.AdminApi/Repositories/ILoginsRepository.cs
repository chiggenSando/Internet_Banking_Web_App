using Mcba.Data.Dtos;

namespace Mcba.AdminApi.Repositories;

public interface ILoginsRepository
{
    public Task<IEnumerable<LoginDto>> LoginsAsync();
    public Task<bool> SetLockedAsync(string loginId, bool locked);
}
