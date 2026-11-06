namespace BiblioGest.DataAccess.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public RolUsuario RolUsuario { get; set; }
        public bool Activo { get; set; } = true;
    }
}
