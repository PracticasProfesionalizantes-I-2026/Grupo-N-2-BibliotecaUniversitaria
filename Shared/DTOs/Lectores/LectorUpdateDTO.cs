using System.ComponentModel.DataAnnotations;

namespace BiblioGest.Shared.DTOs.Lectores;

public class LectorUpdateDTO
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Identificador { get; set; } = string.Empty;
}
