using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/prestamos")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(typeof(IReadOnlyList<PrestamoResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVencidos(CancellationToken ct)
    {
        var vencidos = await _prestamoService.GetVencidosAsync(ct);
        return Ok(vencidos);
    }

    [HttpGet("activos")]
    [ProducesResponseType(typeof(IReadOnlyList<PrestamoResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivos(CancellationToken ct)
    {
        var activos = await _prestamoService.GetActivosAsync(ct);
        return Ok(activos);
    }

    [HttpGet("estado-lector/{lectorId:int}")]
    [ProducesResponseType(typeof(EstadoLectorPrestamoDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(PrestamoResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(PrestamoResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(typeof(PrestamoResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(typeof(PrestamoResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(typeof(PrestamoResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(typeof(PrestamoResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
