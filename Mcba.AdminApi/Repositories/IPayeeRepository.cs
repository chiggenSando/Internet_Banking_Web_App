using Mcba.Data.Models;

namespace Mcba.AdminApi.Repositories;

/// <summary>
/// Data access for payees.
/// </summary>
public interface IPayeeRepository
{
    /// <summary>
    /// Gets payees ordered by name.
    /// </summary>
    /// <param name="state">State to filter by (case-insensitive), or <c>null</c>/blank for all payees.</param>
    public Task<IEnumerable<Payee>> GetAllAsync(string? state);

    /// <summary>
    /// Gets a payee by ID.
    /// </summary>
    /// <param name="id">The payee ID.</param>
    /// <returns>The payee, or <c>null</c> if it does not exist.</returns>
    public Task<Payee?> GetAsync(int id);

    /// <summary>
    /// Saves a new payee.
    /// </summary>
    /// <param name="payee">The payee to add.</param>
    /// <returns>The saved payee with its generated <see cref="Payee.PayeeID"/>.</returns>
    public Task<Payee> AddAsync(Payee payee);
}
