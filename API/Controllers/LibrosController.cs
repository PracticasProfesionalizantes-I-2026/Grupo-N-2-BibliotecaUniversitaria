using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Libros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/v1/libros")]
[Authorize]
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
        var libro = await _libroService.GetByIdAsync(id, ct);
        return Ok(libro);
    }

    // POST /api/v1/libros
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LibroCreateDTO dto, CancellationToken ct)
    {
        var creado = await _libroService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    // PUT /api/v1/libros/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LibroUpdateDTO dto, CancellationToken ct)
    {
        var actualizado = await _libroService.UpdateAsync(id, dto, ct);
        return Ok(actualizado);
    }

    // DELETE /api/v1/libros/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _libroService.DeleteAsync(id, ct);
        return Ok(new { message = "Libro eliminado correctamente." });
    }
}
