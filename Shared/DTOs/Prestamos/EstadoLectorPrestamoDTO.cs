namespace BiblioGest.Shared.DTOs.Prestamos;

public class EstadoLectorPrestamoDTO
{
    public int PrestamosActivos { get; set; }
    public int MaximoPrestamos { get; set; }
    public bool EnMora { get; set; }
    public bool TieneLibroExtraviado { get; set; }
    public DateTime? SuspendidoHasta { get; set; }
    public bool PuedePedir { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
}
