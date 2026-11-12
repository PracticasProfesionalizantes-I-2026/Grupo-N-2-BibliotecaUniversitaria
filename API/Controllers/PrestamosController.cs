using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Authorize]
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

    // GET /api/prestamos/activos?pageNumber=&pageSize=
    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos([FromQuery] int? pageNumber, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var activos = await _prestamoService.GetActivosAsync(pageNumber, pageSize, ct);
        return Ok(activos);
    }

    [HttpGet("estado-lector/{lectorId:int}")]
    public async Task<IActionResult> GetEstadoLector(int lectorId, CancellationToken ct)
    {
        try
        {
            var estado = await _prestamoService.GetEstadoLectorAsync(lectorId, ct);
            return Ok(estado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/prestamos/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
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
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/prestamos/{id}/devolucion
    [HttpPut("{id:int}/devolucion")]
    public async Task<IActionResult> RegistrarDevolucion(int id, CancellationToken ct)
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
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/extravio")]
    public async Task<IActionResult> RegistrarExtravio(int id, CancellationToken ct)
    {
        try
        {
            var actualizado = await _prestamoService.RegistrarExtravioAsync(id, ct);
            return Ok(actualizado);
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

    [HttpPut("{id:int}/reposicion")]
    public async Task<IActionResult> RegistrarReposicion(int id, CancellationToken ct)
    {
        try
        {
            var actualizado = await _prestamoService.RegistrarReposicionAsync(id, ct);
            return Ok(actualizado);
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

    [HttpPut("{id:int}/renovacion")]
    public async Task<IActionResult> Renovar(int id, CancellationToken ct)
    {
        try
        {
            var renovado = await _prestamoService.RenovarAsync(id, ct);
            return Ok(renovado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
