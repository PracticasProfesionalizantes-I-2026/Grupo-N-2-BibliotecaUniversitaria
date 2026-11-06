namespace BiblioGest.Shared.Exceptions;

public class LibroConReservasActivasException : ConflictException
{
    public LibroConReservasActivasException(int libroId)
        : base($"El libro con Id {libroId} no puede eliminarse porque tiene reservas activas.")
    {
    }
}
