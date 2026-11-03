namespace BiblioGest.Shared.Exceptions;

// El usuario autenticado intenta desactivarse o quitarse el rol Administrador
// a sí mismo (PUT /api/usuarios/{id}).
public class UsuarioAutoModificacionException : ConflictException
{
    public UsuarioAutoModificacionException()
        : base("No podés desactivar tu propia cuenta ni quitarte el rol de Administrador.")
    {
    }
}
