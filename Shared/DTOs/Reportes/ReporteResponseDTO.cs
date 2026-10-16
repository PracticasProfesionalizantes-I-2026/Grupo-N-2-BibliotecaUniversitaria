using BiblioGest.Shared.DTOs.Prestamos;

namespace BiblioGest.Shared.DTOs.Reportes;

public class ReporteResponseDTO
{
    public string Tipo { get; set; } = string.Empty;
    public DateTime GeneradoEn { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Mensaje { get; set; }
    public ReporteResumenDTO? Resumen { get; set; }
    public IReadOnlyList<PrestamoResponseDTO> Prestamos { get; set; } = [];
}
