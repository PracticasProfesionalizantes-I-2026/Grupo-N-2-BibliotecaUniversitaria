namespace BiblioGest.Shared.Exceptions;

public class IdentificadorDuplicadoException : ConflictException
{
    public IdentificadorDuplicadoException(string identificador)
        : base($"Ya existe un lector registrado con el identificador (DNI/legajo) '{identificador}'.")
    {
    }
}
