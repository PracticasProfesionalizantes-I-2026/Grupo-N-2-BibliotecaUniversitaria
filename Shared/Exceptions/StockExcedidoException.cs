namespace BiblioGest.Shared.Exceptions;

public class StockExcedidoException : ValidationException
{
    public StockExcedidoException(int stockMaximo)
        : base($"El stock no puede superar los {stockMaximo} ejemplares por libro.")
    {
    }
}
