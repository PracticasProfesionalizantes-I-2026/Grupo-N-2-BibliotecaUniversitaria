using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.Exceptions;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class LibroServiceTests
{
    private readonly Mock<ILibroRepository> _libroRepository = new();
    private readonly Mock<IPrestamoRepository> _prestamoRepository = new();

    private LibroService CrearService() => new(_libroRepository.Object, _prestamoRepository.Object);

    [Fact]
    public async Task CreateLibroAsync_WithValidData_SavesAndReturnsCreatedLibro()
    {
        var dto = new LibroCreateDTO
        {
            Titulo = "Martín Fierro",
            Autor = "José Hernández",
            Isbn = "978-9500000000",
            Ubicacion = "Estante B1",
            Stock = 5
        };

        _libroRepository
            .Setup(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Libro l, CancellationToken _) =>
            {
                l.Id = Guid.NewGuid();
                return l;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(dto.Titulo, resultado.Titulo);
        Assert.Equal(dto.Stock, resultado.Stock);
        _libroRepository.Verify(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLibroAsync_WithMissingRequiredField_ThrowsValidationException()
    {
        var dto = new LibroCreateDTO
        {
            Titulo = "",
            Autor = "Autor",
            Isbn = "123",
            Ubicacion = "A1",
            Stock = 1
        };

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
        _libroRepository.Verify(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLibroAsync_WithNegativeStock_ThrowsValidationException()
    {
        var dto = new LibroCreateDTO
        {
            Titulo = "Titulo",
            Autor = "Autor",
            Isbn = "123",
            Ubicacion = "A1",
            Stock = -1
        };

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task GetLibroByIdAsync_WhenLibroDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Libro?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroNotFoundException>(() => service.GetByIdAsync(id));
    }

    [Fact]
    public async Task UpdateLibroAsync_WithValidData_UpdatesLibro()
    {
        var id = Guid.NewGuid();
        var libroExistente = new Libro { Id = id, Titulo = "Viejo", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libroExistente);

        var dto = new LibroUpdateDTO { Titulo = "Nuevo", Autor = "B", Isbn = "2", Ubicacion = "A2", Stock = 4 };
        var service = CrearService();

        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("Nuevo", resultado.Titulo);
        Assert.Equal(4, resultado.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLibroAsync_WithoutActiveLoans_DeletesLibro()
    {
        var id = Guid.NewGuid();
        var libro = new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.TieneActivosPorLibroAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CrearService();
        await service.DeleteAsync(id);

        _libroRepository.Verify(r => r.DeleteAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLibroAsync_WhenLibroHasActiveLoans_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var libro = new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.TieneActivosPorLibroAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroConPrestamosActivosException>(() => service.DeleteAsync(id));
        _libroRepository.Verify(r => r.DeleteAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
