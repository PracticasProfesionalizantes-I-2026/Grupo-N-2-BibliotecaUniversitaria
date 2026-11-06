namespace BiblioGest.DataAccess.Entities;

public class Reserva
{
    public int Id { get; set; }

    public int LectorId { get; set; }
    public Lector? Lector { get; set; }

    public int LibroId { get; set; }
    public Libro? Libro { get; set; }

    public DateTime FechaReserva { get; set; }
    public DateTime? FechaAsignacion { get; set; }
    public DateTime? FechaLimiteRetiro { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.EnCola;
}
