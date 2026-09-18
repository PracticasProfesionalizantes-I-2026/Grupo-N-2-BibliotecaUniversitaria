namespace BiblioGest.Shared.DTOs.Prestamos;

public class PrestamoCreateDTO
{
    public Guid LectorId { get; set; }
    public Guid LibroId { get; set; }
}
