namespace BiblioGest.Shared.Exceptions;

public class PrestamoNoExtraviadoException : ConflictException
{
    public PrestamoNoExtraviadoException(int prestamoId, string estadoActual)
        : base($"Solo se puede registrar la reposición de un préstamo extraviado. El préstamo con Id {prestamoId} está en estado {estadoActual}.")
    {
    }
}
