using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Prestamos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/v1/prestamos")]
[Authorize]
public class PrestamosController : ControllerBase
{
    private readonly IPrestamoService _prestamoService;

    public PrestamosController(IPrestamoService prestamoService)
    {
        _prestamoService = prestamoService;
    }

    // GET /api/v1/prestamos/mora
    // Nota: se declara antes de "{id}" para que la ruta literal tenga prioridad.
    [HttpGet("mora")]
    public async Task<IActionResult> GetVencidos(CancellationToken ct)
    {
        var vencidos = await _prestamoService.GetVencidosAsync(ct);
        return Ok(vencidos);
    }

    // GET /api/v1/prestamos/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var prestamo = await _prestamoService.GetByIdAsync(id, ct);
        return Ok(prestamo);
    }

    // POST /api/v1/prestamos
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrestamoCreateDTO dto, CancellationToken ct)
    {
        var creado = await _prestamoService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    // PUT /api/v1/prestamos/{id}/devolucion
    [HttpPut("{id:guid}/devolucion")]
    public async Task<IActionResult> RegistrarDevolucion(Guid id, CancellationToken ct)
    {
        var actualizado = await _prestamoService.RegistrarDevolucionAsync(id, ct);
        return Ok(actualizado);
    }
}
