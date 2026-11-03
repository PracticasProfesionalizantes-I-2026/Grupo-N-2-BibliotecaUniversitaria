namespace BiblioGest.Shared.Exceptions;

public class LibroConHistorialPrestamosException : ConflictException
{
    public LibroConHistorialPrestamosException(Guid libroId)
        : base($"El libro con Id '{libroId}' no puede eliminarse porque tiene préstamos registrados en su historial.")
    {
    }
}
