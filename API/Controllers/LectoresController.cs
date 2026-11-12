using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lectores")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class LectoresController : ControllerBase
{
    private readonly ILectorService _lectorService;

    public LectoresController(ILectorService lectorService)
    {
        _lectorService = lectorService;
    }

    // GET /api/lectores?pageNumber=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LectorResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? pageNumber, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var lectores = await _lectorService.GetAllAsync(pageNumber, pageSize, ct);
        return Ok(lectores);
    }

    // GET /api/lectores/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LectorResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
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
    [ProducesResponseType(typeof(LectorResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LectorResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] LectorUpdateDTO dto, CancellationToken ct)
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
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
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
