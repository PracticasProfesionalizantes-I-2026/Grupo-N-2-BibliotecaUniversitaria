namespace BiblioGest.Shared.Exceptions;

public class LibroConPrestamosActivosException : ConflictException
{
    public LibroConPrestamosActivosException(int libroId)
        : base($"El libro con Id {libroId} no puede eliminarse porque tiene préstamos activos.")
    {
    }
}
