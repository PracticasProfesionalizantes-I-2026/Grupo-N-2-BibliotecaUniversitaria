using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Lectores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/v1/lectores")]
[Authorize]
public class LectoresController : ControllerBase
{
    private readonly ILectorService _lectorService;

    public LectoresController(ILectorService lectorService)
    {
        _lectorService = lectorService;
    }

    // GET /api/v1/lectores
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var lectores = await _lectorService.GetAllAsync(ct);
        return Ok(lectores);
    }

    // GET /api/v1/lectores/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var lector = await _lectorService.GetByIdAsync(id, ct);
        return Ok(lector);
    }

    // POST /api/v1/lectores
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LectorCreateDTO dto, CancellationToken ct)
    {
        var creado = await _lectorService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    // PUT /api/v1/lectores/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LectorUpdateDTO dto, CancellationToken ct)
    {
        var actualizado = await _lectorService.UpdateAsync(id, dto, ct);
        return Ok(actualizado);
    }

    // DELETE /api/v1/lectores/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _lectorService.DeleteAsync(id, ct);
        return Ok(new { message = "Lector eliminado correctamente." });
    }
}
