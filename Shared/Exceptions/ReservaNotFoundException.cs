namespace BiblioGest.Shared.Exceptions;

public class ReservaNotFoundException : NotFoundException
{
    public ReservaNotFoundException(int id)
        : base($"No se encontró la reserva con Id {id}.")
    {
    }
}
