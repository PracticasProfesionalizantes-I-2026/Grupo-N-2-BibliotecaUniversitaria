namespace BiblioGest.Shared.Exceptions
{
    public class UsuarioDuplicadoException : ConflictException
    {
        public UsuarioDuplicadoException(string message) : base(message)
        {
        }
    }
}
