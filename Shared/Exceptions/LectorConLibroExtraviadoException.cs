namespace BiblioGest.Shared.Exceptions;

public class LectorConLibroExtraviadoException : ForbiddenException
{
    public LectorConLibroExtraviadoException(int lectorId)
        : base($"El lector con Id {lectorId} tiene un libro extraviado sin reponer y no puede solicitar otro préstamo hasta reponer el ejemplar.")
    {
    }
}
