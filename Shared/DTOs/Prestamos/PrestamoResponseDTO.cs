namespace BiblioGest.Shared.DTOs.Prestamos;

public class PrestamoResponseDTO
{
    public int Id { get; set; }
    public int LectorId { get; set; }
    public string LectorNombreCompleto { get; set; } = string.Empty;
    public int LibroId { get; set; }
    public string LibroTitulo { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public int Renovaciones { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EnMora { get; set; }
}
