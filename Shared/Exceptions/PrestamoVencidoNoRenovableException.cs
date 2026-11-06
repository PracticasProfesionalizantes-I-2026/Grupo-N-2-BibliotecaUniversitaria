namespace BiblioGest.Shared.Exceptions;

public class PrestamoVencidoNoRenovableException : ForbiddenException
{
    public PrestamoVencidoNoRenovableException(int prestamoId)
        : base($"El préstamo con Id {prestamoId} está vencido y no puede renovarse: hay que devolverlo.")
    {
    }
}
