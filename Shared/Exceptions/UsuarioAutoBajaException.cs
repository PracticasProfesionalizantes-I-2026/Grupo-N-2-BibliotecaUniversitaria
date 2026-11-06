namespace BiblioGest.Shared.Exceptions;

public class UsuarioAutoBajaException : ConflictException
{
    public UsuarioAutoBajaException()
        : base("No podés eliminar tu propia cuenta.")
    {
    }
}
