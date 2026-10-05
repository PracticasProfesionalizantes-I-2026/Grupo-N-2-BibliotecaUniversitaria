namespace BiblioGest.Shared.Exceptions;

public class LectorEnMoraException : ConflictException
{
    public LectorEnMoraException(Guid lectorId)
        : base($"El lector con Id '{lectorId}' tiene préstamos vencidos y no puede solicitar otro hasta regularizar su situación.")
    {
    }
}
