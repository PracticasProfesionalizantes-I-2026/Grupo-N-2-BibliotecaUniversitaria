namespace BiblioGest.Shared.DTOs.Libros;

public class LibroCreateDTO
{
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int Stock { get; set; }
}
