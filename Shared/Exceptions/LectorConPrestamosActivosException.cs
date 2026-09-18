namespace BiblioGest.Shared.Exceptions;

public class LectorConPrestamosActivosException : ConflictException
{
    public LectorConPrestamosActivosException(Guid lectorId)
        : base($"El lector con Id '{lectorId}' no puede eliminarse porque tiene préstamos activos.")
    {
    }
}
