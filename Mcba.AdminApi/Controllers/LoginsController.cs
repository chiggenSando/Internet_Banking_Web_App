using Mcba.AdminApi.Repositories;
using Mcba.Data.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginsController(ILoginsRepository _repo) : ControllerBase
{
    [HttpPut("{id}/lock")]
    public async Task<IActionResult> Lock(string id) => await SetLocked(id, true);

    [HttpPut("{id}/unlock")]
    public async Task<IActionResult> Unlock(string id) => await SetLocked(id, false);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoginDto>>> GetAll() => Ok(await _repo.LoginsAsync());

    private async Task<IActionResult> SetLocked(string id, bool locked)
    {
        if (!await _repo.SetLockedAsync(id, locked))
            return NotFound();
        return NoContent();
    }

}
