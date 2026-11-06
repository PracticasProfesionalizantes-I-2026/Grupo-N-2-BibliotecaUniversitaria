namespace BiblioGest.Shared.Exceptions;

public class UltimoAdministradorException : ConflictException
{
    public UltimoAdministradorException()
        : base("No se puede desactivar ni cambiar el rol del último administrador activo.")
    {
    }
}
