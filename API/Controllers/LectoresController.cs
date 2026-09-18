using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/lectores")]
public class LectoresController : ControllerBase
{
    private readonly ILectorService _lectorService;

    public LectoresController(ILectorService lectorService)
    {
        _lectorService = lectorService;
    }

    // GET /api/lectores
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var lectores = await _lectorService.GetAllAsync(ct);
        return Ok(lectores);
    }

    // GET /api/lectores/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var lector = await _lectorService.GetByIdAsync(id, ct);
            return Ok(lector);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST /api/lectores
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LectorCreateDTO dto, CancellationToken ct)
    {
        try
        {
            var creado = await _lectorService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/lectores/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LectorUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            var actualizado = await _lectorService.UpdateAsync(id, dto, ct);
            return Ok(actualizado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/lectores/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _lectorService.DeleteAsync(id, ct);
            return Ok(new { message = "Lector eliminado correctamente." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
