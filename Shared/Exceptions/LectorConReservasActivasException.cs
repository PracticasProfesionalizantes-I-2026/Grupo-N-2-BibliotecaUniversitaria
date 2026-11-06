namespace BiblioGest.Shared.Exceptions;

public class LectorConReservasActivasException : ConflictException
{
    public LectorConReservasActivasException(int lectorId)
        : base($"El lector con Id {lectorId} no puede eliminarse porque tiene reservas activas.")
    {
    }
}
