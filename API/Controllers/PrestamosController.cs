using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/prestamos")]
public class PrestamosController : ControllerBase
{
    private readonly IPrestamoService _prestamoService;

    public PrestamosController(IPrestamoService prestamoService)
    {
        _prestamoService = prestamoService;
    }

    // GET /api/prestamos/mora
    // Nota: se declara antes de "{id}" para que la ruta literal tenga prioridad.
    [HttpGet("mora")]
    public async Task<IActionResult> GetVencidos(CancellationToken ct)
    {
        var vencidos = await _prestamoService.GetVencidosAsync(ct);
        return Ok(vencidos);
    }

    // GET /api/prestamos/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var prestamo = await _prestamoService.GetByIdAsync(id, ct);
            return Ok(prestamo);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST /api/prestamos
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrestamoCreateDTO dto, CancellationToken ct)
    {
        try
        {
            var creado = await _prestamoService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
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

    // PUT /api/prestamos/{id}/devolucion
    [HttpPut("{id:guid}/devolucion")]
    public async Task<IActionResult> RegistrarDevolucion(Guid id, CancellationToken ct)
    {
        try
        {
            var actualizado = await _prestamoService.RegistrarDevolucionAsync(id, ct);
            return Ok(actualizado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
