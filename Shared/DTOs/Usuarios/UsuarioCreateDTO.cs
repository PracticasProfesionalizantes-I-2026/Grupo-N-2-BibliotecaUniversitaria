namespace BiblioGest.Shared.DTOs.Usuarios;

public class UsuarioCreateDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RolUsuario { get; set; } = string.Empty;

}
