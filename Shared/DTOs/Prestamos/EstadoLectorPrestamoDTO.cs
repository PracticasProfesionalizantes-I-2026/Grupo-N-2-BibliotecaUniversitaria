namespace BiblioGest.Shared.DTOs.Prestamos;

// Estado de un lector frente a un nuevo préstamo (RN-10, CU-05 3a/3b), calculado
// por la API para que el frontend sólo lo muestre.
public class EstadoLectorPrestamoDTO
{
    public int PrestamosActivos { get; set; }
    public int MaximoPrestamos { get; set; }
    public bool EnMora { get; set; }
    public bool PuedePedir { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
}
