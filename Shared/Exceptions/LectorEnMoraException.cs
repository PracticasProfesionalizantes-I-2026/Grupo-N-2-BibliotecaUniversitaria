namespace BiblioGest.Shared.Exceptions;

public class LectorEnMoraException : ForbiddenException
{
    public LectorEnMoraException(int lectorId)
        : base($"El lector con Id {lectorId} tiene préstamos vencidos y no puede solicitar otro hasta regularizar su situación.")
    {
    }
}
