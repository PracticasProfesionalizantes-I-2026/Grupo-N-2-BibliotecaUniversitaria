namespace BiblioGest.Shared.Exceptions;

public class ClaimInvalidoException : UnauthorizedException
{
    public ClaimInvalidoException()
        : base("El token no contiene un identificador de usuario válido.")
    {
    }
}
