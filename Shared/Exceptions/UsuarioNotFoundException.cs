namespace BiblioGest.Shared.Exceptions
{
    public class UsuarioNotFoundException : NotFoundException
    {
        public UsuarioNotFoundException(string message) : base(message)
        {
        }   
    }
}
