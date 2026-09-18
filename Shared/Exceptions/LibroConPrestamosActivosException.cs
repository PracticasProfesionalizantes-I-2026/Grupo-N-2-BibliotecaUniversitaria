namespace BiblioGest.Shared.Exceptions;

public class LibroConPrestamosActivosException : ConflictException
{
    public LibroConPrestamosActivosException(Guid libroId)
        : base($"El libro con Id '{libroId}' no puede eliminarse porque tiene préstamos activos.")
    {
    }
}
