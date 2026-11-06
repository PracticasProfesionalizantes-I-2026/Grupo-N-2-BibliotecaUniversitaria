namespace BiblioGest.Shared.Exceptions;

public class PrestamoActivoDuplicadoException : ConflictException
{
    public PrestamoActivoDuplicadoException(int lectorId, int libroId)
        : base($"El lector con Id {lectorId} ya tiene un préstamo activo del libro con Id {libroId}.")
    {
    }
}
