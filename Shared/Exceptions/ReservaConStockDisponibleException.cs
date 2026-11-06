namespace BiblioGest.Shared.Exceptions;

public class ReservaConStockDisponibleException : ConflictException
{
    public ReservaConStockDisponibleException(int libroId, int stock)
        : base($"El libro con Id {libroId} tiene {stock} ejemplar(es) disponible(s): no hace falta reservar, registrá el préstamo directamente.")
    {
    }
}
