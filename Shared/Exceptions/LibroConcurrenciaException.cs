namespace BiblioGest.Shared.Exceptions;

public class LibroConcurrenciaException : ConflictException
{
    public LibroConcurrenciaException(int libroId)
        : base($"El libro con Id {libroId} fue modificado por otra operación simultánea. Reintente la operación.")
    {
    }
}
