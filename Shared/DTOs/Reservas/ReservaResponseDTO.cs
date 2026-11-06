namespace BiblioGest.Shared.DTOs.Reservas;

public class ReservaResponseDTO
{
    public int Id { get; set; }
    public int LectorId { get; set; }
    public string LectorNombreCompleto { get; set; } = string.Empty;
    public int LibroId { get; set; }
    public string LibroTitulo { get; set; } = string.Empty;
    public DateTime FechaReserva { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? PosicionEnCola { get; set; }
    public DateTime? FechaAsignacion { get; set; }
    public DateTime? FechaLimiteRetiro { get; set; }
}
