namespace BiblioGest.Shared.DTOs.Usuarios
{
    public class UsuarioUpdateDTO
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? RolUsuario { get; set; } 
        public bool? Activo { get; set; } 

    }
}
