namespace BiblioGest.Shared.Exceptions;

public class ReservaDuplicadaException : ConflictException
{
    public ReservaDuplicadaException(int lectorId, int libroId)
        : base($"El lector con Id {lectorId} ya tiene una reserva activa para el libro con Id {libroId}.")
    {
    }
}
