namespace BiblioGest.Shared.Exceptions;

public class PrestamoInvalidoException : ValidationException
{
    public PrestamoInvalidoException(string message) : base(message)
    {
    }
}
