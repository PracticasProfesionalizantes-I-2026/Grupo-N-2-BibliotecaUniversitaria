namespace BiblioGest.Shared.Exceptions;

public class LectorNotFoundException : NotFoundException
{
    public LectorNotFoundException(int id)
        : base($"No se encontró el lector con Id {id}.")
    {
    }
}
