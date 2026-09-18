namespace BiblioGest.Shared.Exceptions;

public class LibroNotFoundException : NotFoundException
{
    public LibroNotFoundException(Guid id)
        : base($"No se encontró el libro con Id '{id}'.")
    {
    }
}
