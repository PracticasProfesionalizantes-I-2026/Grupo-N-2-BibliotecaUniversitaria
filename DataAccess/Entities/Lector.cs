namespace BiblioGest.DataAccess.Entities;

public class Lector
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Identificador { get; set; } = string.Empty;

    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
