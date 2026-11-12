namespace BiblioGest.DataAccess.Entities;

public class Libro
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;

    // ISBN sin guiones y en mayúsculas, para comparar unicidad sin aplicar
    // funciones sobre la columna en el WHERE (ver LibroRepository.ExisteIsbnAsync).
    public string IsbnNormalizado { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int Stock { get; set; }

    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
