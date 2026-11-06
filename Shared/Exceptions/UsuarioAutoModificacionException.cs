namespace BiblioGest.Shared.Exceptions;

public class UsuarioAutoModificacionException : ConflictException
{
    public UsuarioAutoModificacionException()
        : base("No podés desactivar tu propia cuenta ni quitarte el rol de Administrador.")
    {
    }
}
