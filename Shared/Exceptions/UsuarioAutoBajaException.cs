namespace BiblioGest.Shared.Exceptions;

// El usuario autenticado intenta eliminar su propia cuenta (DELETE /api/usuarios/{id}).
public class UsuarioAutoBajaException : ConflictException
{
    public UsuarioAutoBajaException()
        : base("No podés eliminar tu propia cuenta.")
    {
    }
}
