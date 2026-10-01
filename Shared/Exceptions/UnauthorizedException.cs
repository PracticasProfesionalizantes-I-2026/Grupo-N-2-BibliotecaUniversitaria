namespace BiblioGest.Shared.Exceptions;

public abstract class UnauthorizedException : Exception
{
    protected UnauthorizedException(string message) : base(message)
    {
    }
}
