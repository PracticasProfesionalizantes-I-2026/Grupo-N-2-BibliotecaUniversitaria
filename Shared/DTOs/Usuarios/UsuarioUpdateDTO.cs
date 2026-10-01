using System.ComponentModel.DataAnnotations;

namespace BiblioGest.Shared.DTOs.Usuarios;

public class UsuarioUpdateDTO
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    // Opcional: si viene vacío o null, el password actual no se modifica.
    public string? Password { get; set; }

    [Required]
    [RegularExpression("^(Bibliotecario|Administrador)$", ErrorMessage = "El rol debe ser 'Bibliotecario' o 'Administrador'.")]
    public string Rol { get; set; } = string.Empty;
}
