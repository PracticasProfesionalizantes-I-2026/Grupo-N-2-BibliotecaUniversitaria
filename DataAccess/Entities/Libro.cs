using System.Security.Cryptography;

namespace BiblioGest.DataAccess.Entities;

public class Libro
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int Stock { get; set; }

    // Token de concurrencia optimista: SQLite no genera rowversion solo, así
    // que se regenera a mano en cada UPDATE (ver LibroRepository.UpdateAsync).
    public byte[] RowVersion { get; set; } = RandomNumberGenerator.GetBytes(8);

    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
