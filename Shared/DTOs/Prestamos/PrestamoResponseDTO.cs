namespace BiblioGest.Shared.DTOs.Prestamos;

public class PrestamoResponseDTO
{
    public Guid Id { get; set; }
    public Guid LectorId { get; set; }
    public string LectorNombreCompleto { get; set; } = string.Empty;
    public Guid LibroId { get; set; }
    public string LibroTitulo { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EnMora { get; set; }
}
