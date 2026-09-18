namespace BiblioGest.Shared.Exceptions;

public class LectorNotFoundException : NotFoundException
{
    public LectorNotFoundException(Guid id)
        : base($"No se encontró el lector con Id '{id}'.")
    {
    }
}
