namespace BiblioGest.Shared.Exceptions;

public class LibroConHistorialException : ConflictException
{
    public LibroConHistorialException(int id)
        : base($"El libro con Id {id} no puede eliminarse porque tiene historial de préstamos.")
    {
    }
}
