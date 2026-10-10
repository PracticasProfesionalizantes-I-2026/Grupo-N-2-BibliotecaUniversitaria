namespace BiblioGest.Shared.DTOs.Usuarios
{
    public class UsuarioResponseDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string RolUsuario { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
