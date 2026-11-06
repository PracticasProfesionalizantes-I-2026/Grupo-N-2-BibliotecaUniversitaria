namespace BiblioGest.Shared.DTOs.Libros;

public class LibroUpdateDTO
{
    public string? Titulo { get; set; }
    public string? Autor { get; set; }
    public string? Isbn { get; set; }
    public string? Ubicacion { get; set; }
    public int? Stock { get; set; }
}
