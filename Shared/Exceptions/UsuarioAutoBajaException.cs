namespace BiblioGest.Shared.Exceptions;

public class UsuarioAutoBajaException : ConflictException
{
    public UsuarioAutoBajaException()
        : base("Un administrador no puede darse de baja ni desactivarse a sí mismo.")
    {
    }
}
