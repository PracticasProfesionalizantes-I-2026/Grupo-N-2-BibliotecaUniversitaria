namespace BiblioGest.Shared.Exceptions;

// Mismo mensaje/código para usuario inexistente, inactivo o contraseña
// incorrecta (CU-01, 3a/3b): no hay que revelar cuál dato es el inválido.
public class CredencialesInvalidasException : UnauthorizedException
{
    public CredencialesInvalidasException()
        : base("Email o contraseña incorrectos.")
    {
    }
}
