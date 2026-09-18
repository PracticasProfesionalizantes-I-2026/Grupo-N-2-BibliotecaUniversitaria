namespace BiblioGest.Shared.Exceptions;

public class StockInsuficienteException : ConflictException
{
    public StockInsuficienteException(Guid libroId)
        : base($"El libro con Id '{libroId}' no tiene stock disponible para préstamo.")
    {
    }
}
