namespace BiblioGest.Shared.Exceptions;

public class LectorConHistorialPrestamosException : ConflictException
{
    public LectorConHistorialPrestamosException(Guid lectorId)
        : base($"El lector con Id '{lectorId}' no puede eliminarse porque tiene préstamos registrados en su historial.")
    {
    }
}
