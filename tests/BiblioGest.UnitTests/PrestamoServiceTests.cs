using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class PrestamoServiceTests
{
    private readonly Mock<IPrestamoRepository> _prestamoRepository = new();
    private readonly Mock<ILibroRepository> _libroRepository = new();
    private readonly Mock<ILectorRepository> _lectorRepository = new();
    private readonly Mock<IReservaRepository> _reservaRepository = new();
    private readonly Mock<IColaDeReservas> _colaDeReservas = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PrestamoService CrearService() =>
        new(_prestamoRepository.Object, _libroRepository.Object, _lectorRepository.Object, _reservaRepository.Object, _colaDeReservas.Object, _unitOfWork.Object);

    private static Lector CrearLector(int id) =>
        new() { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Dni = "1" };

    private static Libro CrearLibro(int id, int stock) =>
        new() { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = stock };

    [Fact]
    public async Task CreatePrestamoAsync_WithValidData_SavesAndReturnsCreatedPrestamo()
    {
        var lectorId = IdsDePrueba.Nuevo();
        var libroId = IdsDePrueba.Nuevo();
        var lector = CrearLector(lectorId);
        var libro = CrearLibro(libroId, stock: 2);

        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _libroRepository.Setup(r => r.GetByIdAsync(libroId, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _prestamoRepository.Setup(r => r.LectorTieneMoraAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _prestamoRepository
            .Setup(r => r.CreateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prestamo p, CancellationToken _) =>
            {
                p.Id = IdsDePrueba.Nuevo();
                return p;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId });

        Assert.True(resultado.Id > 0);
        Assert.Equal("Activo", resultado.Estado);
        Assert.Equal(resultado.FechaPrestamo.AddDays(14), resultado.FechaVencimiento);
        Assert.Equal(1, libro.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLibroStockWasChangedConcurrently_ThrowsLibroConcurrenciaExceptionAndRollsBack()
    {
        var lectorId = IdsDePrueba.Nuevo();
        var libroId = IdsDePrueba.Nuevo();
        var lector = CrearLector(lectorId);
        var libro = CrearLibro(libroId, stock: 1);

        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _libroRepository.Setup(r => r.GetByIdAsync(libroId, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _prestamoRepository.Setup(r => r.LectorTieneMoraAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _prestamoRepository
            .Setup(r => r.CreateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prestamo p, CancellationToken _) =>
            {
                p.Id = IdsDePrueba.Nuevo();
                return p;
            });
        // Simula que, entre la lectura y el descuento, otra petición ya
        // actualizó el libro: EF detecta el RowVersion desactualizado.
        _libroRepository
            .Setup(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var service = CrearService();

        await Assert.ThrowsAsync<LibroConcurrenciaException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId }));

        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorOrLibroNotFound_ThrowsNotFoundException()
    {
        var lectorId = IdsDePrueba.Nuevo();
        var libroId = IdsDePrueba.Nuevo();
        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync((Lector?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorNotFoundException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId }));
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorHasThreeActiveLoans_ThrowsForbiddenException()
    {
        var lectorId = IdsDePrueba.Nuevo();
        var libroId = IdsDePrueba.Nuevo();
        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLector(lectorId));
        _libroRepository.Setup(r => r.GetByIdAsync(libroId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLibro(libroId, stock: 3));
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var service = CrearService();

        await Assert.ThrowsAsync<LimitePrestamosActivosException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId }));
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorHasOverdueLoans_ThrowsForbiddenException()
    {
        var lectorId = IdsDePrueba.Nuevo();
        var libroId = IdsDePrueba.Nuevo();
        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLector(lectorId));
        _libroRepository.Setup(r => r.GetByIdAsync(libroId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLibro(libroId, stock: 3));
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _prestamoRepository.Setup(r => r.LectorTieneMoraAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorEnMoraException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId }));
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLibroHasNoStock_ThrowsConflictException()
    {
        var lectorId = IdsDePrueba.Nuevo();
        var libroId = IdsDePrueba.Nuevo();
        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLector(lectorId));
        _libroRepository.Setup(r => r.GetByIdAsync(libroId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLibro(libroId, stock: 0));
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _prestamoRepository.Setup(r => r.LectorTieneMoraAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CrearService();

        await Assert.ThrowsAsync<StockInsuficienteException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId }));
    }

    [Fact]
    public async Task RegisterDevolucionAsync_WithValidData_UpdatesStockAndPrestamoState()
    {
        var id = IdsDePrueba.Nuevo();
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0);
        var prestamo = new Prestamo
        {
            Id = id,
            LectorId = IdsDePrueba.Nuevo(),
            LibroId = libro.Id,
            Libro = libro,
            FechaPrestamo = DateTime.UtcNow.AddDays(-5),
            FechaVencimiento = DateTime.UtcNow.AddDays(9),
            Estado = EstadoPrestamo.Activo
        };
        _prestamoRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(prestamo);

        var service = CrearService();
        var resultado = await service.RegistrarDevolucionAsync(id);

        Assert.Equal("Devuelto", resultado.Estado);
        Assert.NotNull(resultado.FechaDevolucion);
        Assert.Equal(1, libro.Stock);
        _prestamoRepository.Verify(r => r.UpdateAsync(prestamo, It.IsAny<CancellationToken>(), false), Times.Once);
        _libroRepository.Verify(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>(), false), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterDevolucionAsync_WhenUpdatingStockFails_NeverReachesSaveChanges()
    {
        var id = IdsDePrueba.Nuevo();
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0);
        var prestamo = new Prestamo
        {
            Id = id,
            LectorId = IdsDePrueba.Nuevo(),
            LibroId = libro.Id,
            Libro = libro,
            FechaPrestamo = DateTime.UtcNow.AddDays(-5),
            FechaVencimiento = DateTime.UtcNow.AddDays(9),
            Estado = EstadoPrestamo.Activo
        };
        _prestamoRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(prestamo);
        // Simula que falla el paso intermedio (actualizar el stock del libro
        // al devolver) después de haber marcado el préstamo como Devuelto.
        _libroRepository
            .Setup(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>(), false))
            .ThrowsAsync(new InvalidOperationException("Fallo simulado al actualizar el stock."));

        var service = CrearService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarDevolucionAsync(id));

        // El fallo ocurre antes del SaveChanges final: nada quedó persistido.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterDevolucionAsync_WhenAlreadyReturned_ThrowsConflictException()
    {
        var id = IdsDePrueba.Nuevo();
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 1);
        var prestamo = new Prestamo
        {
            Id = id,
            LectorId = IdsDePrueba.Nuevo(),
            LibroId = libro.Id,
            Libro = libro,
            FechaPrestamo = DateTime.UtcNow.AddDays(-5),
            FechaVencimiento = DateTime.UtcNow.AddDays(9),
            FechaDevolucion = DateTime.UtcNow.AddDays(-1),
            Estado = EstadoPrestamo.Devuelto
        };
        _prestamoRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(prestamo);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoYaDevueltoException>(() => service.RegistrarDevolucionAsync(id));
        Assert.Equal(1, libro.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPrestamosVencidosAsync_ReturnsOverdueList()
    {
        var vencido = new Prestamo
        {
            Id = IdsDePrueba.Nuevo(),
            LectorId = IdsDePrueba.Nuevo(),
            LibroId = IdsDePrueba.Nuevo(),
            Lector = CrearLector(IdsDePrueba.Nuevo()),
            Libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0),
            FechaPrestamo = DateTime.UtcNow.AddDays(-20),
            FechaVencimiento = DateTime.UtcNow.AddDays(-6),
            Estado = EstadoPrestamo.Activo
        };
        _prestamoRepository.Setup(r => r.GetVencidosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prestamo> { vencido });

        var service = CrearService();
        var resultado = await service.GetVencidosAsync();

        Assert.Single(resultado);
        Assert.True(resultado[0].EnMora);
    }

    [Fact]
    public async Task GetPrestamosVencidosAsync_WithNoOverdueLoans_ReturnsEmptyList()
    {
        _prestamoRepository.Setup(r => r.GetVencidosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prestamo>());

        var service = CrearService();
        var resultado = await service.GetVencidosAsync();

        Assert.Empty(resultado);
    }

    private void ConfigurarAltaSinBloqueos(Lector lector, Libro libro)
    {
        _lectorRepository.Setup(r => r.GetByIdAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _libroRepository.Setup(r => r.GetByIdAsync(libro.Id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _prestamoRepository.Setup(r => r.LectorTieneMoraAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _prestamoRepository
            .Setup(r => r.CreateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prestamo p, CancellationToken _) =>
            {
                p.Id = IdsDePrueba.Nuevo();
                return p;
            });
    }

    private static Prestamo CrearPrestamoActivo(Lector lector, Libro libro, DateTime fechaVencimiento) => new()
    {
        Id = IdsDePrueba.Nuevo(),
        LectorId = lector.Id,
        Lector = lector,
        LibroId = libro.Id,
        Libro = libro,
        FechaPrestamo = fechaVencimiento.AddDays(-14),
        FechaVencimiento = fechaVencimiento,
        Estado = EstadoPrestamo.Activo
    };

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorIsSuspended_ThrowsForbiddenException()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        lector.SuspendidoHasta = DateTime.UtcNow.AddDays(2);
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 3);
        ConfigurarAltaSinBloqueos(lector, libro);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorSuspendidoException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id }));
        _prestamoRepository.Verify(r => r.CreateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenSuspensionHasExpired_CreatesPrestamo()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        lector.SuspendidoHasta = DateTime.UtcNow.AddMinutes(-1);
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 3);
        ConfigurarAltaSinBloqueos(lector, libro);

        var service = CrearService();
        var resultado = await service.CreateAsync(new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id });

        Assert.Equal("Activo", resultado.Estado);
    }

    [Fact]
    public async Task RegisterDevolucionAsync_WithThreeDaysLate_SuspendsLectorForThreeDays()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0);
        var prestamo = CrearPrestamoActivo(lector, libro, DateTime.UtcNow.AddDays(-3));
        _prestamoRepository.Setup(r => r.GetByIdAsync(prestamo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prestamo);

        var service = CrearService();
        var resultado = await service.RegistrarDevolucionAsync(prestamo.Id);

        Assert.Equal(resultado.FechaDevolucion!.Value.AddDays(3), lector.SuspendidoHasta);
        _lectorRepository.Verify(r => r.UpdateAsync(lector, It.IsAny<CancellationToken>(), false), Times.Once);
    }

    [Fact]
    public async Task RegisterDevolucionAsync_OnTime_DoesNotSuspendLector()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0);
        var prestamo = CrearPrestamoActivo(lector, libro, DateTime.UtcNow.AddDays(1));
        _prestamoRepository.Setup(r => r.GetByIdAsync(prestamo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prestamo);

        var service = CrearService();
        await service.RegistrarDevolucionAsync(prestamo.Id);

        Assert.Null(lector.SuspendidoHasta);
        _lectorRepository.Verify(r => r.UpdateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterDevolucionAsync_WhenLectorAlreadySuspended_AddsDaysToCurrentSuspension()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        var suspensionActual = DateTime.UtcNow.AddDays(5);
        lector.SuspendidoHasta = suspensionActual;
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0);
        var prestamo = CrearPrestamoActivo(lector, libro, DateTime.UtcNow.AddDays(-2));
        _prestamoRepository.Setup(r => r.GetByIdAsync(prestamo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prestamo);

        var service = CrearService();
        await service.RegistrarDevolucionAsync(prestamo.Id);

        Assert.Equal(suspensionActual.AddDays(2), lector.SuspendidoHasta);
    }

    private Prestamo ConfigurarPrestamo(EstadoPrestamo estado, int stockLibro, DateTime fechaVencimiento, out Lector lector, out Libro libro)
    {
        lector = CrearLector(IdsDePrueba.Nuevo());
        libro = CrearLibro(IdsDePrueba.Nuevo(), stock: stockLibro);
        var prestamo = CrearPrestamoActivo(lector, libro, fechaVencimiento);
        prestamo.Id = IdsDePrueba.Nuevo();
        prestamo.Estado = estado;
        _prestamoRepository.Setup(r => r.GetByIdAsync(prestamo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prestamo);
        return prestamo;
    }

    [Fact]
    public async Task RegistrarExtravioAsync_WithActivePrestamo_MarksExtraviadoAndKeepsStock()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Activo, stockLibro: 0, DateTime.UtcNow.AddDays(5), out _, out var libro);

        var service = CrearService();
        var resultado = await service.RegistrarExtravioAsync(prestamo.Id);

        Assert.Equal("Extraviado", resultado.Estado);
        Assert.Null(resultado.FechaDevolucion);
        Assert.Equal(0, libro.Stock);
        _prestamoRepository.Verify(r => r.UpdateAsync(prestamo, It.IsAny<CancellationToken>()), Times.Once);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoPrestamo.Devuelto)]
    [InlineData(EstadoPrestamo.Extraviado)]
    [InlineData(EstadoPrestamo.Repuesto)]
    public async Task RegistrarExtravioAsync_WhenPrestamoIsNotActive_ThrowsConflictException(EstadoPrestamo estado)
    {
        var prestamo = ConfigurarPrestamo(estado, stockLibro: 1, DateTime.UtcNow.AddDays(5), out _, out _);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoNoActivoException>(() => service.RegistrarExtravioAsync(prestamo.Id));
        _prestamoRepository.Verify(r => r.UpdateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarExtravioAsync_WithUnknownId_ThrowsNotFoundException()
    {
        _prestamoRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prestamo?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoNotFoundException>(() => service.RegistrarExtravioAsync(IdsDePrueba.Nuevo()));
    }

    [Fact]
    public async Task RegistrarReposicionAsync_WithExtraviadoPrestamo_MarksRepuestoAndRestoresStock()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Extraviado, stockLibro: 0, DateTime.UtcNow.AddDays(5), out var lector, out var libro);

        var service = CrearService();
        var resultado = await service.RegistrarReposicionAsync(prestamo.Id);

        Assert.Equal("Repuesto", resultado.Estado);
        Assert.NotNull(resultado.FechaDevolucion);
        Assert.Equal(1, libro.Stock);
        Assert.Null(lector.SuspendidoHasta);
        _libroRepository.Verify(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>(), false), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarReposicionAsync_WhenUpdatingStockFails_NeverReachesSaveChanges()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Extraviado, stockLibro: 0, DateTime.UtcNow.AddDays(5), out _, out var libro);
        // Simula que falla el paso intermedio (actualizar el stock del libro
        // al reponer) después de haber marcado el préstamo como Repuesto.
        _libroRepository
            .Setup(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>(), false))
            .ThrowsAsync(new InvalidOperationException("Fallo simulado al actualizar el stock."));

        var service = CrearService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarReposicionAsync(prestamo.Id));

        // El fallo ocurre antes del SaveChanges final: nada quedó persistido.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarReposicionAsync_WhenLate_SuspendsLectorForDaysLate()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Extraviado, stockLibro: 0, DateTime.UtcNow.AddDays(-4), out var lector, out _);

        var service = CrearService();
        var resultado = await service.RegistrarReposicionAsync(prestamo.Id);

        Assert.Equal(resultado.FechaDevolucion!.Value.AddDays(4), lector.SuspendidoHasta);
    }

    [Theory]
    [InlineData(EstadoPrestamo.Activo)]
    [InlineData(EstadoPrestamo.Devuelto)]
    [InlineData(EstadoPrestamo.Repuesto)]
    public async Task RegistrarReposicionAsync_WhenPrestamoIsNotExtraviado_ThrowsConflictException(EstadoPrestamo estado)
    {
        var prestamo = ConfigurarPrestamo(estado, stockLibro: 0, DateTime.UtcNow.AddDays(5), out _, out var libro);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoNoExtraviadoException>(() => service.RegistrarReposicionAsync(prestamo.Id));
        Assert.Equal(0, libro.Stock);
    }

    [Fact]
    public async Task RegisterDevolucionAsync_WhenPrestamoIsExtraviado_ThrowsConflictException()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Extraviado, stockLibro: 0, DateTime.UtcNow.AddDays(5), out _, out var libro);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoNoActivoException>(() => service.RegistrarDevolucionAsync(prestamo.Id));
        Assert.Equal(0, libro.Stock);
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorHasExtraviadoPrestamo_ThrowsForbiddenException()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 3);
        ConfigurarAltaSinBloqueos(lector, libro);
        _prestamoRepository.Setup(r => r.LectorTieneExtraviadosAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorConLibroExtraviadoException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id }));
        _prestamoRepository.Verify(r => r.CreateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePrestamoAsync_WithAssignedReserva_UsesHeldCopyWithoutStock()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0);
        ConfigurarAltaSinBloqueos(lector, libro);
        var reserva = new Reserva { Id = IdsDePrueba.Nuevo(), LectorId = lector.Id, LibroId = libro.Id, Estado = EstadoReserva.Asignada };
        _reservaRepository.Setup(r => r.GetAsignadaAsync(lector.Id, libro.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reserva);

        var service = CrearService();
        var resultado = await service.CreateAsync(new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id });

        Assert.Equal("Activo", resultado.Estado);
        Assert.Equal(EstadoReserva.Concretada, reserva.Estado);
        Assert.Equal(0, libro.Stock);
        _reservaRepository.Verify(r => r.UpdateAsync(reserva, It.IsAny<CancellationToken>()), Times.Once);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePrestamoAsync_ProcessesExpiredAssignmentsBeforeCheckingStock()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 0);
        ConfigurarAltaSinBloqueos(lector, libro);
        _colaDeReservas
            .Setup(c => c.ProcesarAsignacionesVencidasAsync(libro, It.IsAny<CancellationToken>()))
            .Callback(() => libro.Stock = 1)
            .Returns(Task.CompletedTask);

        var service = CrearService();
        var resultado = await service.CreateAsync(new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id });

        Assert.Equal("Activo", resultado.Estado);
        Assert.Equal(0, libro.Stock);
    }

    [Fact]
    public async Task RegisterDevolucionAsync_WhenQueueHasReaders_AssignsCopyInsteadOfRestoringStock()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Activo, stockLibro: 0, DateTime.UtcNow.AddDays(5), out _, out var libro);
        _colaDeReservas
            .Setup(c => c.AsignarAlSiguienteAsync(libro, It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(true);

        var service = CrearService();
        var resultado = await service.RegistrarDevolucionAsync(prestamo.Id);

        Assert.Equal("Devuelto", resultado.Estado);
        Assert.Equal(0, libro.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RenovarAsync_WithActivePrestamo_ExtendsDueDateFourteenDays()
    {
        var vencimiento = DateTime.UtcNow.AddDays(3);
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Activo, stockLibro: 0, vencimiento, out _, out _);

        var service = CrearService();
        var resultado = await service.RenovarAsync(prestamo.Id);

        Assert.Equal(vencimiento.AddDays(14), resultado.FechaVencimiento);
        Assert.Equal(1, resultado.Renovaciones);
        _prestamoRepository.Verify(r => r.UpdateAsync(prestamo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenovarAsync_WhenAlreadyRenewedTwice_ThrowsForbiddenException()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Activo, stockLibro: 0, DateTime.UtcNow.AddDays(3), out _, out _);
        prestamo.Renovaciones = 2;

        var service = CrearService();

        await Assert.ThrowsAsync<LimiteRenovacionesException>(() => service.RenovarAsync(prestamo.Id));
        _prestamoRepository.Verify(r => r.UpdateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RenovarAsync_WhenLibroHasActiveReservas_ThrowsForbiddenException()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Activo, stockLibro: 0, DateTime.UtcNow.AddDays(3), out _, out var libro);
        _reservaRepository.Setup(r => r.TieneActivasPorLibroAsync(libro.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<RenovacionConReservasException>(() => service.RenovarAsync(prestamo.Id));
        Assert.Equal(0, prestamo.Renovaciones);
    }

    [Fact]
    public async Task RenovarAsync_WhenPrestamoIsOverdue_ThrowsForbiddenException()
    {
        var prestamo = ConfigurarPrestamo(EstadoPrestamo.Activo, stockLibro: 0, DateTime.UtcNow.AddDays(-1), out _, out _);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoVencidoNoRenovableException>(() => service.RenovarAsync(prestamo.Id));
    }

    [Theory]
    [InlineData(EstadoPrestamo.Devuelto)]
    [InlineData(EstadoPrestamo.Extraviado)]
    [InlineData(EstadoPrestamo.Repuesto)]
    public async Task RenovarAsync_WhenPrestamoIsNotActive_ThrowsConflictException(EstadoPrestamo estado)
    {
        var prestamo = ConfigurarPrestamo(estado, stockLibro: 0, DateTime.UtcNow.AddDays(3), out _, out _);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoNoActivoException>(() => service.RenovarAsync(prestamo.Id));
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorAlreadyHasSameBookActive_ThrowsConflictException()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        var libro = CrearLibro(IdsDePrueba.Nuevo(), stock: 3);
        ConfigurarAltaSinBloqueos(lector, libro);
        _prestamoRepository.Setup(r => r.ExistePrestamoActivoAsync(lector.Id, libro.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<PrestamoActivoDuplicadoException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id }));
        _prestamoRepository.Verify(r => r.CreateAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetEstadoLectorAsync_WithoutRestrictions_CanBorrow()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        _lectorRepository.Setup(r => r.GetByIdAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = CrearService();
        var estado = await service.GetEstadoLectorAsync(lector.Id);

        Assert.True(estado.PuedePedir);
        Assert.Null(estado.Motivo);
        Assert.Equal(1, estado.PrestamosActivos);
        Assert.Equal(3, estado.MaximoPrestamos);
        Assert.Equal(estado.FechaPrestamo.AddDays(14), estado.FechaVencimiento);
    }

    [Fact]
    public async Task GetEstadoLectorAsync_WhenSuspended_CannotBorrowAndExplainsWhy()
    {
        var lector = CrearLector(IdsDePrueba.Nuevo());
        lector.SuspendidoHasta = DateTime.UtcNow.AddDays(2);
        _lectorRepository.Setup(r => r.GetByIdAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);

        var service = CrearService();
        var estado = await service.GetEstadoLectorAsync(lector.Id);

        Assert.False(estado.PuedePedir);
        Assert.Equal(lector.SuspendidoHasta, estado.SuspendidoHasta);
        Assert.Contains("suspendido", estado.Motivo);
    }

    [Fact]
    public async Task GetEstadoLectorAsync_WithUnknownLector_ThrowsNotFoundException()
    {
        var service = CrearService();

        await Assert.ThrowsAsync<LectorNotFoundException>(() => service.GetEstadoLectorAsync(IdsDePrueba.Nuevo()));
    }
}
