namespace BiblioGest.Shared.Exceptions;

public class RenovacionConReservasException : ForbiddenException
{
    public RenovacionConReservasException(int prestamoId)
        : base($"El préstamo con Id {prestamoId} no puede renovarse porque hay lectores en la cola de reservas de ese libro.")
    {
    }
}
