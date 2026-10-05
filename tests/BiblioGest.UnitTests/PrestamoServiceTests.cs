using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class PrestamoServiceTests
{
    private readonly Mock<IPrestamoRepository> _prestamoRepository = new();
    private readonly Mock<ILibroRepository> _libroRepository = new();
    private readonly Mock<ILectorRepository> _lectorRepository = new();

    private PrestamoService CrearService() =>
        new(_prestamoRepository.Object, _libroRepository.Object, _lectorRepository.Object);

    private static Lector CrearLector(Guid id) =>
        new() { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Identificador = "1" };

    private static Libro CrearLibro(Guid id, int stock) =>
        new() { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = stock };

    [Fact]
    public async Task CreatePrestamoAsync_WithValidData_SavesAndReturnsCreatedPrestamo()
    {
        var lectorId = Guid.NewGuid();
        var libroId = Guid.NewGuid();
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
                p.Id = Guid.NewGuid();
                return p;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId });

        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("Activo", resultado.Estado);
        Assert.Equal(resultado.FechaPrestamo.AddDays(14), resultado.FechaVencimiento);
        Assert.Equal(1, libro.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorOrLibroNotFound_ThrowsNotFoundException()
    {
        var lectorId = Guid.NewGuid();
        var libroId = Guid.NewGuid();
        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync((Lector?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorNotFoundException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId }));
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorHasThreeActiveLoans_ThrowsConflictException()
    {
        var lectorId = Guid.NewGuid();
        var libroId = Guid.NewGuid();
        _lectorRepository.Setup(r => r.GetByIdAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLector(lectorId));
        _libroRepository.Setup(r => r.GetByIdAsync(libroId, It.IsAny<CancellationToken>())).ReturnsAsync(CrearLibro(libroId, stock: 3));
        _prestamoRepository.Setup(r => r.CountActivosPorLectorAsync(lectorId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var service = CrearService();

        await Assert.ThrowsAsync<LimitePrestamosActivosException>(
            () => service.CreateAsync(new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId }));
    }

    [Fact]
    public async Task CreatePrestamoAsync_WhenLectorHasOverdueLoans_ThrowsConflictException()
    {
        var lectorId = Guid.NewGuid();
        var libroId = Guid.NewGuid();
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
        var lectorId = Guid.NewGuid();
        var libroId = Guid.NewGuid();
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
        var id = Guid.NewGuid();
        var libro = CrearLibro(Guid.NewGuid(), stock: 0);
        var prestamo = new Prestamo
        {
            Id = id,
            LectorId = Guid.NewGuid(),
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
        _prestamoRepository.Verify(r => r.UpdateAsync(prestamo, It.IsAny<CancellationToken>()), Times.Once);
        _libroRepository.Verify(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPrestamosVencidosAsync_ReturnsOverdueList()
    {
        var vencido = new Prestamo
        {
            Id = Guid.NewGuid(),
            LectorId = Guid.NewGuid(),
            LibroId = Guid.NewGuid(),
            Lector = CrearLector(Guid.NewGuid()),
            Libro = CrearLibro(Guid.NewGuid(), stock: 0),
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
}
