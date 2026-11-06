namespace BiblioGest.Shared.Exceptions;

public class LectorConHistorialException : ConflictException
{
    public LectorConHistorialException(int id)
        : base($"El lector con Id {id} no puede eliminarse porque tiene historial de préstamos.")
    {
    }
}
