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

    private static LibroCreateDTO NuevoLibroDto(string? isbn = null) => new()
    {
        Titulo = "Martín Fierro",
        Autor = "José Hernández",
        Isbn = isbn ?? IsbnTestHelper.NuevoIsbnValido(),
        Ubicacion = "Estante B1",
        Stock = 5
    };

    private void SetupSinIsbnDuplicado() =>
        _libroRepository
            .Setup(r => r.GetByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Libro?)null);

    [Fact]
    public async Task CreateLibroAsync_WithValidData_SavesAndReturnsCreatedLibro()
    {
        var dto = NuevoLibroDto();
        SetupSinIsbnDuplicado();

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
        var dto = NuevoLibroDto();
        dto.Titulo = "";

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
        _libroRepository.Verify(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLibroAsync_WithNegativeStock_ThrowsValidationException()
    {
        var dto = NuevoLibroDto();
        dto.Stock = -1;

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("9780307474729")]
    [InlineData("97803074747")]
    [InlineData("abcdefghij")]
    public async Task CreateLibroAsync_WithInvalidIsbn_ThrowsValidationException(string isbnInvalido)
    {
        var dto = NuevoLibroDto(isbnInvalido);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLibroAsync_WithHyphensAndSpacesInIsbn_StripsThemBeforeSaving()
    {
        var isbn = IsbnTestHelper.NuevoIsbnValido();
        var isbnConGuiones = $"{isbn[..3]}-{isbn[3..6]}-{isbn[6..]}";
        var dto = NuevoLibroDto(isbnConGuiones);
        SetupSinIsbnDuplicado();
        _libroRepository
            .Setup(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Libro l, CancellationToken _) =>
            {
                l.Id = Guid.NewGuid();
                return l;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.Equal(isbn, resultado.Isbn);
    }

    [Fact]
    public async Task CreateLibroAsync_WithTooLongTitulo_ThrowsValidationException()
    {
        var dto = NuevoLibroDto();
        dto.Titulo = new string('A', 201);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLibroAsync_WhenIsbnAlreadyExists_ThrowsConflictException()
    {
        var isbn = IsbnTestHelper.NuevoIsbnValido();
        var dto = NuevoLibroDto(isbn);
        _libroRepository
            .Setup(r => r.GetByIsbnAsync(isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Libro { Id = Guid.NewGuid(), Isbn = isbn });

        var service = CrearService();

        await Assert.ThrowsAsync<IsbnDuplicadoException>(() => service.CreateAsync(dto));
        _libroRepository.Verify(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLibroAsync_WhenIsbnDuplicadoWithHyphensAndSpaces_ThrowsConflictException()
    {
        var isbn = IsbnTestHelper.NuevoIsbnValido();
        var isbnConEspacios = $" {isbn[..3]} - {isbn[3..]} ";
        var dto = NuevoLibroDto(isbnConEspacios);
        _libroRepository
            .Setup(r => r.GetByIsbnAsync(isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Libro { Id = Guid.NewGuid(), Isbn = isbn });

        var service = CrearService();

        await Assert.ThrowsAsync<IsbnDuplicadoException>(() => service.CreateAsync(dto));
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
        var isbnOriginal = IsbnTestHelper.NuevoIsbnValido();
        var isbnNuevo = IsbnTestHelper.NuevoIsbnValido();
        var libroExistente = new Libro { Id = id, Titulo = "Viejo", Autor = "A", Isbn = isbnOriginal, Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libroExistente);
        _libroRepository.Setup(r => r.GetByIsbnAsync(isbnNuevo, It.IsAny<CancellationToken>())).ReturnsAsync((Libro?)null);

        var dto = new LibroUpdateDTO { Titulo = "Nuevo", Autor = "B", Isbn = isbnNuevo, Ubicacion = "A2", Stock = 4 };
        var service = CrearService();

        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("Nuevo", resultado.Titulo);
        Assert.Equal(4, resultado.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLibroAsync_KeepingSameIsbn_DoesNotThrowConflictException()
    {
        var id = Guid.NewGuid();
        var isbn = IsbnTestHelper.NuevoIsbnValido();
        var libroExistente = new Libro { Id = id, Titulo = "Viejo", Autor = "A", Isbn = isbn, Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libroExistente);
        _libroRepository.Setup(r => r.GetByIsbnAsync(isbn, It.IsAny<CancellationToken>())).ReturnsAsync(libroExistente);

        var dto = new LibroUpdateDTO { Titulo = "Nuevo", Autor = "A", Isbn = isbn, Ubicacion = "A1", Stock = 1 };
        var service = CrearService();

        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("Nuevo", resultado.Titulo);
    }

    [Fact]
    public async Task DeleteLibroAsync_WithoutLoanHistory_DeletesLibro()
    {
        var id = Guid.NewGuid();
        var libro = new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = IsbnTestHelper.NuevoIsbnValido(), Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.TienePrestamosPorLibroAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CrearService();
        await service.DeleteAsync(id);

        _libroRepository.Verify(r => r.DeleteAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLibroAsync_WhenLibroHasLoanHistory_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var libro = new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = IsbnTestHelper.NuevoIsbnValido(), Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.TienePrestamosPorLibroAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroConHistorialPrestamosException>(() => service.DeleteAsync(id));
        _libroRepository.Verify(r => r.DeleteAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchLibrosAsync_WithText_ReturnsMatchingLibrosWithDisponibilidad()
    {
        _libroRepository
            .Setup(r => r.GetAllAsync("borges", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Libro>
            {
                new() { Id = Guid.NewGuid(), Titulo = "Ficciones", Autor = "Borges", Isbn = "1", Ubicacion = "A1", Stock = 2 },
                new() { Id = Guid.NewGuid(), Titulo = "El Aleph", Autor = "Borges", Isbn = "2", Ubicacion = "A2", Stock = 0 }
            });

        var service = CrearService();
        var resultado = await service.GetAllAsync("borges");

        Assert.Equal(2, resultado.Count);
        Assert.True(resultado[0].Disponible);
        Assert.Equal("Disponible (2 ejemplares)", resultado[0].DisponibilidadTexto);
        Assert.False(resultado[1].Disponible);
        Assert.Equal("Sin stock", resultado[1].DisponibilidadTexto);
    }

    [Fact]
    public async Task SearchLibrosAsync_WithNoMatches_ReturnsEmptyList()
    {
        _libroRepository
            .Setup(r => r.GetAllAsync("inexistente", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Libro>());

        var service = CrearService();
        var resultado = await service.GetAllAsync("inexistente");

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task GetLibroDisponibilidadAsync_WithZeroStock_ReturnsUnavailable()
    {
        var id = Guid.NewGuid();
        _libroRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Libro { Id = id, Titulo = "Rayuela", Autor = "Cortázar", Isbn = "3", Ubicacion = "B1", Stock = 0 });

        var service = CrearService();
        var resultado = await service.GetByIdAsync(id);

        Assert.Equal(0, resultado.Stock);
        Assert.False(resultado.Disponible);
        Assert.Equal("Sin stock", resultado.DisponibilidadTexto);
    }
}
