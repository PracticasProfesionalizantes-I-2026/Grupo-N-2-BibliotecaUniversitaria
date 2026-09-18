namespace BiblioGest.Shared.Exceptions;

public class LimitePrestamosActivosException : ConflictException
{
    public LimitePrestamosActivosException(Guid lectorId)
        : base($"El lector con Id '{lectorId}' ya tiene 3 préstamos activos y no puede solicitar otro.")
    {
    }
}
