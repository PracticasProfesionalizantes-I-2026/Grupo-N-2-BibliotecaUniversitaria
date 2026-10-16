using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.DTOs.Reportes;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class ReporteService : IReporteService
{
    private const string TipoResumen = "resumen";
    private const string TipoPrestamos = "prestamos";
    private const string TipoMora = "mora";
    private const string MensajeSinDatos = "No hay datos disponibles para el reporte.";

    private static readonly IReadOnlyList<ReporteTipoDTO> Tipos =
    [
        new() { Tipo = TipoResumen, Descripcion = "Totales de libros, lectores y préstamos por estado." },
        new() { Tipo = TipoPrestamos, Descripcion = "Préstamos realizados, filtrables por rango de fechas (desde/hasta)." },
        new() { Tipo = TipoMora, Descripcion = "Préstamos activos vencidos (lectores en mora)." }
    ];

    private readonly ILibroRepository _libroRepository;
    private readonly ILectorRepository _lectorRepository;
    private readonly IPrestamoRepository _prestamoRepository;

    public ReporteService(
        ILibroRepository libroRepository,
        ILectorRepository lectorRepository,
        IPrestamoRepository prestamoRepository)
    {
        _libroRepository = libroRepository;
        _lectorRepository = lectorRepository;
        _prestamoRepository = prestamoRepository;
    }

    public IReadOnlyList<ReporteTipoDTO> GetTipos() => Tipos;

    public async Task<ReporteResponseDTO> GetReporteAsync(string tipo, DateTime? desde, DateTime? hasta, CancellationToken ct = default)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
        {
            throw new ReporteInvalidoException("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");
        }

        var tipoNormalizado = tipo?.Trim().ToLowerInvariant() ?? string.Empty;

        return tipoNormalizado switch
        {
            TipoResumen => await GenerarResumenAsync(ct),
            TipoPrestamos => await GenerarPrestamosAsync(desde, hasta, ct),
            TipoMora => await GenerarMoraAsync(ct),
            _ => throw new ReporteInvalidoException(
                $"El tipo de reporte '{tipo}' no existe. Tipos válidos: {TipoResumen}, {TipoPrestamos}, {TipoMora}.")
        };
    }

    private async Task<ReporteResponseDTO> GenerarResumenAsync(CancellationToken ct)
    {
        var libros = await _libroRepository.GetAllAsync(null, ct);
        var lectores = await _lectorRepository.GetAllAsync(ct);
        var prestamos = await _prestamoRepository.GetPorRangoAsync(null, null, ct);
        var vencidos = await _prestamoRepository.GetVencidosAsync(ct);

        var resumen = new ReporteResumenDTO
        {
            TotalLibros = libros.Count,
            LibrosSinStock = libros.Count(l => l.Stock == 0),
            TotalLectores = lectores.Count,
            PrestamosActivos = prestamos.Count(p => p.Estado == EstadoPrestamo.Activo),
            PrestamosDevueltos = prestamos.Count(p => p.Estado == EstadoPrestamo.Devuelto),
            PrestamosVencidos = vencidos.Count
        };

        var sinDatos = libros.Count == 0 && lectores.Count == 0 && prestamos.Count == 0;

        return new ReporteResponseDTO
        {
            Tipo = TipoResumen,
            GeneradoEn = DateTime.UtcNow,
            Resumen = resumen,
            Mensaje = sinDatos ? MensajeSinDatos : null
        };
    }

    private async Task<ReporteResponseDTO> GenerarPrestamosAsync(DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        var desdeDia = desde?.Date;
        var hastaExclusivo = hasta?.Date.AddDays(1);

        var prestamos = await _prestamoRepository.GetPorRangoAsync(desdeDia, hastaExclusivo, ct);

        return CrearReporteDePrestamos(TipoPrestamos, prestamos, desdeDia, hasta?.Date);
    }

    private async Task<ReporteResponseDTO> GenerarMoraAsync(CancellationToken ct)
    {
        var vencidos = await _prestamoRepository.GetVencidosAsync(ct);

        return CrearReporteDePrestamos(TipoMora, vencidos, null, null);
    }

    private static ReporteResponseDTO CrearReporteDePrestamos(
        string tipo, IReadOnlyList<Prestamo> prestamos, DateTime? desde, DateTime? hasta) => new()
    {
        Tipo = tipo,
        GeneradoEn = DateTime.UtcNow,
        Desde = desde,
        Hasta = hasta,
        Prestamos = prestamos.Select(MapToResponseDTO).ToList(),
        Mensaje = prestamos.Count == 0 ? MensajeSinDatos : null
    };

    private static PrestamoResponseDTO MapToResponseDTO(Prestamo prestamo) => new()
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
        Estado = prestamo.Estado.ToString(),
        EnMora = prestamo.Estado == EstadoPrestamo.Activo && prestamo.FechaVencimiento < DateTime.UtcNow
    };
}
