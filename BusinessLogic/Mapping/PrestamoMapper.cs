using BiblioGest.DataAccess.Entities;
using BiblioGest.Shared.DTOs.Prestamos;

namespace BiblioGest.BusinessLogic.Mapping;

// Centraliza el cálculo de estado/días/mensaje de un préstamo a partir de un único
// "ahora" (provisto por el caller vía TimeProvider), para que la API resuelva toda
// la lógica de fechas y el frontend sólo muestre el resultado.
public static class PrestamoMapper
{
    public const string EstadoActivo = "Activo";
    public const string EstadoVencido = "Vencido";
    public const string EstadoDevuelto = "Devuelto";
    public const string EstadoDevueltoConAtraso = "Devuelto con atraso";

    public static PrestamoResponseDTO ToResponseDTO(Prestamo prestamo, DateTime ahoraUtc)
    {
        var devuelto = prestamo.Estado == EstadoPrestamo.Devuelto;
        var devueltoConAtraso = devuelto
            && prestamo.FechaDevolucion.HasValue
            && prestamo.FechaDevolucion.Value > prestamo.FechaVencimiento;
        var vencido = !devuelto && prestamo.FechaVencimiento < ahoraUtc;

        var estado = devuelto
            ? (devueltoConAtraso ? EstadoDevueltoConAtraso : EstadoDevuelto)
            : (vencido ? EstadoVencido : EstadoActivo);

        int? diasRestantes = devuelto
            ? null
            : Math.Max(0, (prestamo.FechaVencimiento.Date - ahoraUtc.Date).Days);

        var diasDeAtraso = vencido
            ? (ahoraUtc.Date - prestamo.FechaVencimiento.Date).Days
            : devueltoConAtraso
                ? (prestamo.FechaDevolucion!.Value.Date - prestamo.FechaVencimiento.Date).Days
                : 0;

        return new PrestamoResponseDTO
        {
            Id = prestamo.Id,
            LectorId = prestamo.LectorId,
            LectorNombreCompleto = prestamo.Lector is null
                ? string.Empty
                : $"{prestamo.Lector.Nombre} {prestamo.Lector.Apellido}",
            LibroId = prestamo.LibroId,
            LibroTitulo = prestamo.Libro?.Titulo ?? string.Empty,
            FechaPrestamo = prestamo.FechaPrestamo,
            FechaVencimiento = prestamo.FechaVencimiento,
            FechaDevolucion = prestamo.FechaDevolucion,
            Estado = estado,
            EnMora = vencido,
            DiasRestantes = diasRestantes,
            DiasDeAtraso = diasDeAtraso,
            Mensaje = CrearMensaje(estado, diasRestantes, diasDeAtraso)
        };
    }

    private static string CrearMensaje(string estado, int? diasRestantes, int diasDeAtraso) => estado switch
    {
        EstadoDevuelto => "Devuelto a tiempo.",
        EstadoDevueltoConAtraso => $"Devuelto con {diasDeAtraso} {Dia(diasDeAtraso)} de atraso.",
        EstadoVencido => $"Atrasado {diasDeAtraso} {Dia(diasDeAtraso)}.",
        _ => diasRestantes == 0 ? "Vence hoy." : $"Vence en {diasRestantes} {Dia(diasRestantes!.Value)}."
    };

    private static string Dia(int cantidad) => cantidad == 1 ? "día" : "días";
}
