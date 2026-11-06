namespace BiblioGest.Shared.Exceptions;

public class LimiteRenovacionesException : ForbiddenException
{
    public LimiteRenovacionesException(int prestamoId, int maximo)
        : base($"El préstamo con Id {prestamoId} ya fue renovado {maximo} veces, que es el máximo permitido.")
    {
    }
}
