namespace BiblioGest.Shared.DTOs.Lectores;

public class LectorResponseDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public DateTime? SuspendidoHasta { get; set; }
}
