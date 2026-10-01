namespace BiblioGest.Shared.Exceptions;

public class UsuarioNotFoundException : NotFoundException
{
    public UsuarioNotFoundException(Guid id)
        : base($"No se encontró el usuario con Id '{id}'.")
    {
    }
}
