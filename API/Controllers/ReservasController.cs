using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Reservas;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reservas")]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;

    public ReservasController(IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetColaPorLibro([FromQuery] int libroId, CancellationToken ct)
    {
        try
        {
            var cola = await _reservaService.GetColaPorLibroAsync(libroId, ct);
            return Ok(cola);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        try
        {
            var reserva = await _reservaService.GetByIdAsync(id, ct);
            return Ok(reserva);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReservaCreateDTO dto, CancellationToken ct)
    {
        try
        {
            var creada = await _reservaService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = creada.Id }, creada);
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

    [HttpPut("{id:int}/cancelacion")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
    {
        try
        {
            var cancelada = await _reservaService.CancelarAsync(id, ct);
            return Ok(cancelada);
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
