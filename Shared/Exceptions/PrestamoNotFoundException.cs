namespace BiblioGest.Shared.Exceptions;

public class PrestamoNotFoundException : NotFoundException
{
    public PrestamoNotFoundException(int id)
        : base($"No se encontró el préstamo con Id {id}.")
    {
    }
}
