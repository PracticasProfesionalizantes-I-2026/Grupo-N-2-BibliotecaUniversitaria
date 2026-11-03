namespace BiblioGest.Shared.Exceptions;

public class IsbnDuplicadoException : ConflictException
{
    public IsbnDuplicadoException(string isbn)
        : base($"Ya existe un libro registrado con el ISBN '{isbn}'.")
    {
    }
}
