using Mcba.AdminApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginsController(ILoginsRepository _repo) : ControllerBase
{
    [HttpPut("{id}/lock")]
    public async Task<IActionResult> Lock(string id) => await SetLocked(id, true);

    private async Task<IActionResult> SetLocked(string id, bool locked)
    {
        if (!await _repo.SetLockedAsync(id, locked))
            return NotFound();
        return NoContent();
    }
}
