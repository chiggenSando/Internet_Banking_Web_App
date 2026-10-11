using Mcba.Data.Dtos;

namespace Mcba.AdminApi.Repositories;

/// <summary>
/// Data access for customer logins.
/// </summary>
public interface ILoginsRepository
{
    /// <summary>
    /// Gets all logins ordered by login ID, with the owning customer's name.
    /// </summary>
    public Task<IEnumerable<LoginDto>> LoginsAsync();

    /// <summary>
    /// Sets whether a login is locked.
    /// </summary>
    /// <param name="loginId">The 8-digit login ID.</param>
    /// <param name="locked"><c>true</c> to lock the login, <c>false</c> to unlock it.</param>
    /// <returns><c>true</c> if the login was found and updated; <c>false</c> if it does not exist.</returns>
    public Task<bool> SetLockedAsync(string loginId, bool locked);
}
