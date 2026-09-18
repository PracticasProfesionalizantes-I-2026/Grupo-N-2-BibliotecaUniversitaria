namespace BiblioGest.Shared.Exceptions;

public class LectorInvalidoException : ValidationException
{
    public LectorInvalidoException(string message) : base(message)
    {
    }
}
