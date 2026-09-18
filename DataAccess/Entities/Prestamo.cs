namespace BiblioGest.DataAccess.Entities;

public class Prestamo
{
    public Guid Id { get; set; }

    public Guid LectorId { get; set; }
    public Lector? Lector { get; set; }

    public Guid LibroId { get; set; }
    public Libro? Libro { get; set; }

    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Activo;
}
