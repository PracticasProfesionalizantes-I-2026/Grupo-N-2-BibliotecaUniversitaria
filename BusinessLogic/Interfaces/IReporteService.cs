using BiblioGest.Shared.DTOs.Reportes;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IReporteService
{
    IReadOnlyList<ReporteTipoDTO> GetTipos();
    Task<ReporteResponseDTO> GetReporteAsync(string tipo, DateTime? desde, DateTime? hasta, CancellationToken ct = default);
}
