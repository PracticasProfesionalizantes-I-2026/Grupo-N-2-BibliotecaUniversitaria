namespace BiblioGest.Shared.Exceptions;

public class DniDuplicadoException : ConflictException
{
    public DniDuplicadoException(string dni)
        : base($"Ya existe un lector registrado con el DNI '{dni}'.")
    {
    }
}
