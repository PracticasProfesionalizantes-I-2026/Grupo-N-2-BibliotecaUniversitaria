using System.ComponentModel.DataAnnotations;

namespace BiblioGest.Shared.DTOs.Libros;

public class LibroUpdateDTO
{
    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Autor { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Ubicacion { get; set; } = string.Empty;

    public int Stock { get; set; }
}
