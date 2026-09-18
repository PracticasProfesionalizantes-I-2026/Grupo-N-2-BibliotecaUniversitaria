namespace BiblioGest.Shared.Exceptions;

public class LibroInvalidoException : ValidationException
{
    public LibroInvalidoException(string message) : base(message)
    {
    }
}
