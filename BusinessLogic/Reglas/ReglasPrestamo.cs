namespace BiblioGest.BusinessLogic.Reglas;

// Única fuente de los parámetros de préstamo: la usan CreateAsync, el estado del
// lector frente a un préstamo y LectorService, para no duplicar los números.
public static class ReglasPrestamo
{
    // RN-10: máximo de préstamos activos por lector.
    public const int MaximoPrestamosActivosPorLector = 3;

    // RN-12: duración del préstamo en días.
    public const int DiasDePrestamo = 14;
}
