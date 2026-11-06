namespace BiblioGest.Shared.Exceptions;

public class LectorSuspendidoException : ForbiddenException
{
    public LectorSuspendidoException(int lectorId, DateTime suspendidoHasta)
        : base($"El lector con Id {lectorId} está suspendido hasta el {suspendidoHasta:dd/MM/yyyy HH:mm} (UTC) por devolver préstamos con atraso.")
    {
    }
}
