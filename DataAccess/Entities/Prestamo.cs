namespace BiblioGest.DataAccess.Entities;

public class Prestamo
{
    public int Id { get; set; }

    public int LectorId { get; set; }
    public Lector? Lector { get; set; }

    public int LibroId { get; set; }
    public Libro? Libro { get; set; }

    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public int Renovaciones { get; set; }
    public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Activo;
}
