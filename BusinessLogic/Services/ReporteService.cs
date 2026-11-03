using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Mapping;
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
    private readonly TimeProvider _timeProvider;

    public ReporteService(
        ILibroRepository libroRepository,
        ILectorRepository lectorRepository,
        IPrestamoRepository prestamoRepository,
        TimeProvider timeProvider)
    {
        _libroRepository = libroRepository;
        _lectorRepository = lectorRepository;
        _prestamoRepository = prestamoRepository;
        _timeProvider = timeProvider;
    }

    private DateTime AhoraUtc() => _timeProvider.GetUtcNow().UtcDateTime;

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
        var ahoraUtc = AhoraUtc();
        var libros = await _libroRepository.GetAllAsync(null, ct);
        var lectores = await _lectorRepository.GetAllAsync(ct);
        var prestamos = await _prestamoRepository.GetPorRangoAsync(null, null, ct);
        var vencidos = await _prestamoRepository.GetVencidosAsync(ahoraUtc, ct);

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
            GeneradoEn = ahoraUtc,
            Resumen = resumen,
            Mensaje = sinDatos ? MensajeSinDatos : null
        };
    }

    private async Task<ReporteResponseDTO> GenerarPrestamosAsync(DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        var desdeDia = desde?.Date;
        var hastaExclusivo = hasta?.Date.AddDays(1);

        var prestamos = await _prestamoRepository.GetPorRangoAsync(desdeDia, hastaExclusivo, ct);

        return CrearReporteDePrestamos(TipoPrestamos, prestamos, desdeDia, hasta?.Date, AhoraUtc());
    }

    private async Task<ReporteResponseDTO> GenerarMoraAsync(CancellationToken ct)
    {
        var ahoraUtc = AhoraUtc();
        var vencidos = await _prestamoRepository.GetVencidosAsync(ahoraUtc, ct);

        return CrearReporteDePrestamos(TipoMora, vencidos, null, null, ahoraUtc);
    }

    private static ReporteResponseDTO CrearReporteDePrestamos(
        string tipo, IReadOnlyList<Prestamo> prestamos, DateTime? desde, DateTime? hasta, DateTime ahoraUtc) => new()
    {
        Tipo = tipo,
        GeneradoEn = ahoraUtc,
        Desde = desde,
        Hasta = hasta,
        Prestamos = prestamos.Select(p => PrestamoMapper.ToResponseDTO(p, ahoraUtc)).ToList(),
        Mensaje = prestamos.Count == 0 ? MensajeSinDatos : null
    };
}
