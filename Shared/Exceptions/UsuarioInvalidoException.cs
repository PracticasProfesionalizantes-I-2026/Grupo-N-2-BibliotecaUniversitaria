namespace BiblioGest.Shared.Exceptions
{
    public class UsuarioInvalidoException : ValidationException
    {
        public UsuarioInvalidoException(string message) : base(message)
        {
        }
    }
}
