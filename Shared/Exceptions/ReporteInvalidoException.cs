namespace BiblioGest.Shared.Exceptions;

public class ReporteInvalidoException : ValidationException
{
    public ReporteInvalidoException(string message) : base(message)
    {
    }
}
