namespace BiblioGest.Shared.Exceptions;

public class PrestamoNoActivoException : ConflictException
{
    public PrestamoNoActivoException(int prestamoId, string estadoActual)
        : base($"El préstamo con Id {prestamoId} no está activo (estado actual: {estadoActual}).")
    {
    }
}
