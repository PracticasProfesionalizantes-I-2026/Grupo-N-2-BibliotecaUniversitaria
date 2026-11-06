namespace BiblioGest.Shared.Exceptions;

public class ReservaNoActivaException : ConflictException
{
    public ReservaNoActivaException(int id, string estadoActual)
        : base($"La reserva con Id {id} no está activa (estado actual: {estadoActual}).")
    {
    }
}
