using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Reportes;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reportes")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReporteTipoDTO>), StatusCodes.Status200OK)]
    public IActionResult GetTipos()
    {
        return Ok(_reporteService.GetTipos());
    }

    [HttpGet("{tipo}")]
    [ProducesResponseType(typeof(ReporteResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReporte(string tipo, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct)
    {
        try
        {
            var reporte = await _reporteService.GetReporteAsync(tipo, desde, hasta, ct);
            return Ok(reporte);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
