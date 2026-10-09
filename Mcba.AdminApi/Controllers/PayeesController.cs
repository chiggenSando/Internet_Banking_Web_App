using Mcba.AdminApi.Repositories;
using Mcba.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mcba.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayeesController(IPayeeRepository repo) : ControllerBase
{
    private readonly IPayeeRepository _repo = repo;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payee>>> GetAll([FromQuery] string state)
    {
        return Ok(await _repo.GetAllAsync(state));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payee>> Get(int id)
    {
        var payee = await _repo.GetAsync(id);
        if (payee is null)
            return NotFound();
        return Ok(payee);
    }

    [HttpPost]
    public async Task<ActionResult<Payee>> Create(Payee payee)
    {
        var created = await _repo.AddAsync(payee);
        return CreatedAtAction(nameof(Get), new { id = created.PayeeID }, created);
    }
}
