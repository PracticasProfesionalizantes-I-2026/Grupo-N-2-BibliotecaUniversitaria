using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/libros")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class LibrosController : ControllerBase
{
    private readonly ILibroService _libroService;

    public LibrosController(ILibroService libroService)
    {
        _libroService = libroService;
    }

    // GET /api/libros?busqueda=
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LibroResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? busqueda, CancellationToken ct)
    {
        var libros = await _libroService.GetAllAsync(busqueda, ct);
        return Ok(libros);
    }

    // GET /api/libros/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LibroResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        try
        {
            var libro = await _libroService.GetByIdAsync(id, ct);
            return Ok(libro);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST /api/libros
    [HttpPost]
    [ProducesResponseType(typeof(LibroResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] LibroCreateDTO dto, CancellationToken ct)
    {
        try
        {
            var creado = await _libroService.CreateAsync(dto, ct);
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

    // PUT /api/libros/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LibroResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] LibroUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            var actualizado = await _libroService.UpdateAsync(id, dto, ct);
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

    // DELETE /api/libros/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _libroService.DeleteAsync(id, ct);
            return Ok(new { message = "Libro eliminado correctamente." });
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
