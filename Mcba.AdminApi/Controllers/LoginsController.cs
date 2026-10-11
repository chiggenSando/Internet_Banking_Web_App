using Mcba.AdminApi.Repositories;
using Mcba.Data.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminApi.Controllers;

/// <summary>
/// Endpoints for viewing customer logins and locking or unlocking them.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LoginsController(ILoginsRepository _repo) : ControllerBase
{
    /// <summary>
    /// Locks a login so the customer can no longer sign in.
    /// </summary>
    /// <param name="id">The 8-digit login ID.</param>
    /// <response code="204">The login was locked.</response>
    /// <response code="404">No login exists with the given ID.</response>
    [HttpPut("{id}/lock")]
    public async Task<IActionResult> Lock(string id) => await SetLocked(id, true);

    /// <summary>
    /// Unlocks a login so the customer can sign in again.
    /// </summary>
    /// <param name="id">The 8-digit login ID.</param>
    /// <response code="204">The login was unlocked.</response>
    /// <response code="404">No login exists with the given ID.</response>
    [HttpPut("{id}/unlock")]
    public async Task<IActionResult> Unlock(string id) => await SetLocked(id, false);

    /// <summary>
    /// Lists all logins ordered by login ID. Password hashes are not included.
    /// </summary>
    /// <response code="200">All logins.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoginDto>>> GetAll() => Ok(await _repo.LoginsAsync());

    private async Task<IActionResult> SetLocked(string id, bool locked)
    {
        if (!await _repo.SetLockedAsync(id, locked))
            return NotFound();
        return NoContent();
    }

}
