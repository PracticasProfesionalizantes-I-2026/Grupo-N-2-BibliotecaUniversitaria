namespace BiblioGest.Shared.Exceptions;

public class LectorEmailDuplicadoException : ConflictException
{
    public LectorEmailDuplicadoException(string email)
        : base($"Ya existe un lector registrado con el email '{email}'.")
    {
    }
}
