namespace BiblioGest.Shared.Exceptions;

public class StockInsuficienteException : ConflictException
{
    public StockInsuficienteException(int libroId)
        : base($"El libro con Id {libroId} no tiene stock disponible para préstamo.")
    {
    }
}
