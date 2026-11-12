using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.Exceptions;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class ReporteServiceTests
{
    private readonly Mock<ILibroRepository> _libroRepository = new();
    private readonly Mock<ILectorRepository> _lectorRepository = new();
    private readonly Mock<IPrestamoRepository> _prestamoRepository = new();

    private ReporteService CrearService() =>
        new(_libroRepository.Object, _lectorRepository.Object, _prestamoRepository.Object);

    private static Prestamo NuevoPrestamo(EstadoPrestamo estado, DateTime fechaPrestamo) => new()
    {
        Id = IdsDePrueba.Nuevo(),
        LectorId = IdsDePrueba.Nuevo(),
        Lector = new Lector { Nombre = "Ana", Apellido = "Gómez" },
        LibroId = IdsDePrueba.Nuevo(),
        Libro = new Libro { Titulo = "Ficciones" },
        FechaPrestamo = fechaPrestamo,
        FechaVencimiento = fechaPrestamo.AddDays(14),
        Estado = estado
    };

    [Fact]
    public void GetTipos_ReturnsReportesDisponibles()
    {
        var service = CrearService();

        var tipos = service.GetTipos();

        Assert.Equal(["resumen", "prestamos", "mora"], tipos.Select(t => t.Tipo));
    }

    [Fact]
    public async Task GetReporteAsync_WithValidType_ReturnsReportData()
    {
        var hoy = DateTime.UtcNow;
        _libroRepository
            .Setup(r => r.GetAllAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Libro { Stock = 2 }, new Libro { Stock = 0 }]);
        _lectorRepository
            .Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Lector()]);
        _prestamoRepository
            .Setup(r => r.GetPorRangoAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                NuevoPrestamo(EstadoPrestamo.Activo, hoy),
                NuevoPrestamo(EstadoPrestamo.Devuelto, hoy.AddDays(-30)),
                NuevoPrestamo(EstadoPrestamo.Extraviado, hoy.AddDays(-10)),
                NuevoPrestamo(EstadoPrestamo.Extraviado, hoy.AddDays(-12)),
                NuevoPrestamo(EstadoPrestamo.Repuesto, hoy.AddDays(-40))]);
        _prestamoRepository
            .Setup(r => r.GetVencidosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([NuevoPrestamo(EstadoPrestamo.Activo, hoy.AddDays(-20))]);

        var service = CrearService();
        var reporte = await service.GetReporteAsync("resumen", null, null);

        Assert.Equal("resumen", reporte.Tipo);
        Assert.NotNull(reporte.Resumen);
        Assert.Equal(2, reporte.Resumen!.TotalLibros);
        Assert.Equal(1, reporte.Resumen.LibrosSinStock);
        Assert.Equal(1, reporte.Resumen.TotalLectores);
        Assert.Equal(1, reporte.Resumen.PrestamosActivos);
        Assert.Equal(1, reporte.Resumen.PrestamosDevueltos);
        Assert.Equal(2, reporte.Resumen.PrestamosExtraviados);
        Assert.Equal(1, reporte.Resumen.PrestamosRepuestos);
        Assert.Equal(1, reporte.Resumen.PrestamosVencidos);
        Assert.Null(reporte.Mensaje);
    }

    [Fact]
    public async Task GetReporteAsync_Prestamos_WithRange_PassesDatesToRepository()
    {
        var desde = new DateTime(2026, 10, 1, 15, 30, 0);
        var hasta = new DateTime(2026, 10, 10);
        _prestamoRepository
            .Setup(r => r.GetPorRangoAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([NuevoPrestamo(EstadoPrestamo.Activo, new DateTime(2026, 10, 5))]);

        var service = CrearService();
        var reporte = await service.GetReporteAsync("prestamos", desde, hasta);

        Assert.Single(reporte.Prestamos);
        Assert.Equal("Ana Gómez", reporte.Prestamos[0].LectorNombreCompleto);
        _prestamoRepository.Verify(r => r.GetPorRangoAsync(
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 11), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReporteAsync_WithNoData_ReturnsEmptyReport()
    {
        _prestamoRepository
            .Setup(r => r.GetVencidosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CrearService();
        var reporte = await service.GetReporteAsync("mora", null, null);

        Assert.Empty(reporte.Prestamos);
        Assert.Equal("No hay datos disponibles para el reporte.", reporte.Mensaje);
    }

    [Fact]
    public async Task GetReporteAsync_WithInvalidFilter_ThrowsValidationException()
    {
        var service = CrearService();

        await Assert.ThrowsAsync<ReporteInvalidoException>(() =>
            service.GetReporteAsync("prestamos", new DateTime(2026, 10, 10), new DateTime(2026, 10, 1)));
    }

    [Fact]
    public async Task GetReporteAsync_WithUnknownType_ThrowsValidationException()
    {
        var service = CrearService();

        await Assert.ThrowsAsync<ReporteInvalidoException>(() => service.GetReporteAsync("ventas", null, null));
    }

    [Fact]
    public async Task GetReporteAsync_WithRangeLongerThanOneYear_ThrowsReporteInvalidoException()
    {
        var service = CrearService();

        await Assert.ThrowsAsync<ReporteInvalidoException>(() =>
            service.GetReporteAsync("prestamos", new DateTime(2025, 1, 1), new DateTime(2026, 1, 2)));
        _prestamoRepository.Verify(r => r.GetPorRangoAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetReporteAsync_WithRangeOfExactlyOneYear_ReturnsReport()
    {
        _prestamoRepository
            .Setup(r => r.GetPorRangoAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CrearService();
        var reporte = await service.GetReporteAsync("prestamos", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1));

        Assert.Equal("prestamos", reporte.Tipo);
    }
}
