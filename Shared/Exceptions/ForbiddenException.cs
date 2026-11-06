namespace BiblioGest.Shared.Exceptions;

public abstract class ForbiddenException : Exception
{
    protected ForbiddenException(string message) : base(message)
    {
    }
}
