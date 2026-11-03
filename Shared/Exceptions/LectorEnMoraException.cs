namespace BiblioGest.Shared.Exceptions;

public class LectorEnMoraException : ConflictException
{
    public LectorEnMoraException(Guid lectorId)
        : base(CrearMensaje(lectorId))
    {
    }

    // Expuesto para que el estado del lector informe el mismo motivo sin lanzar la excepción.
    public static string CrearMensaje(Guid lectorId) =>
        $"El lector con Id '{lectorId}' tiene préstamos vencidos y no puede solicitar otro hasta regularizar su situación.";
}
