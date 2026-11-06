namespace BiblioGest.Shared.Exceptions;

public class PrestamoYaDevueltoException : ConflictException
{
    public PrestamoYaDevueltoException(int prestamoId)
        : base($"El préstamo con Id {prestamoId} ya fue devuelto.")
    {
    }
}
