namespace BiblioGest.Shared.Exceptions;

public class PrestamoNotFoundException : NotFoundException
{
    public PrestamoNotFoundException(Guid id)
        : base($"No se encontró el préstamo con Id '{id}'.")
    {
    }
}
