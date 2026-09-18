namespace BiblioGest.Shared.Exceptions;

public abstract class ConflictException : Exception
{
    protected ConflictException(string message) : base(message)
    {
    }
}
