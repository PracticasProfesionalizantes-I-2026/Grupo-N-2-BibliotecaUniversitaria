using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/v1/libros")]
public class LibrosController : ControllerBase
{
    private readonly ILibroService _libroService;

    public LibrosController(ILibroService libroService)
    {
        _libroService = libroService;
    }

    // GET /api/v1/libros?busqueda=
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? busqueda, CancellationToken ct)
    {
        var libros = await _libroService.GetAllAsync(busqueda, ct);
        return Ok(libros);
    }

    // GET /api/v1/libros/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
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

    // POST /api/v1/libros
    [HttpPost]
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
    }

    // PUT /api/v1/libros/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LibroUpdateDTO dto, CancellationToken ct)
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
    }

    // DELETE /api/v1/libros/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
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
