namespace BiblioGest.Shared.Exceptions;

public class LimitePrestamosActivosException : ConflictException
{
    public LimitePrestamosActivosException(Guid lectorId, int maximoPrestamos)
        : base(CrearMensaje(lectorId, maximoPrestamos))
    {
    }

    // Expuesto para que el estado del lector informe el mismo motivo sin lanzar la excepción.
    public static string CrearMensaje(Guid lectorId, int maximoPrestamos) =>
        $"El lector con Id '{lectorId}' ya tiene {maximoPrestamos} préstamos activos y no puede solicitar otro.";
}
