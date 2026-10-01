namespace BiblioGest.Shared.Exceptions;

public class EmailDuplicadoException : ConflictException
{
    public EmailDuplicadoException(string email)
        : base($"Ya existe un usuario registrado con el email '{email}'.")
    {
    }
}
