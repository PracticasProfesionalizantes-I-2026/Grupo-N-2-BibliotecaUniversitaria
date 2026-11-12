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
    private readonly Mock<IReservaRepository> _reservaRepository = new();

    private LibroService CrearService() => new(_libroRepository.Object, _prestamoRepository.Object, _reservaRepository.Object);

    [Fact]
    public async Task CreateLibroAsync_WithValidData_SavesAndReturnsCreatedLibro()
    {
        var dto = new LibroCreateDTO
        {
            Titulo = "Martín Fierro",
            Autor = "José Hernández",
            Isbn = "978-9500000000",
            Ubicacion = "B1-1",
            Stock = 5
        };

        _libroRepository
            .Setup(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Libro l, CancellationToken _) =>
            {
                l.Id = IdsDePrueba.Nuevo();
                return l;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.True(resultado.Id > 0);
        Assert.Equal(dto.Titulo, resultado.Titulo);
        Assert.Equal(dto.Stock, resultado.Stock);
        _libroRepository.Verify(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Once);
        _libroRepository.Verify(
            r => r.CreateAsync(It.Is<Libro>(l => l.IsbnNormalizado == "9789500000000"), It.IsAny<CancellationToken>()),
            Times.Once);
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
        var id = IdsDePrueba.Nuevo();
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Libro?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroNotFoundException>(() => service.GetByIdAsync(id));
    }

    [Fact]
    public async Task UpdateLibroAsync_WithValidData_UpdatesLibro()
    {
        var id = IdsDePrueba.Nuevo();
        var libroExistente = new Libro { Id = id, Titulo = "Viejo", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libroExistente);

        var dto = new LibroUpdateDTO { Titulo = "Nuevo", Autor = "B", Isbn = "978-8420633109", Ubicacion = "A2-1", Stock = 4 };
        var service = CrearService();

        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("Nuevo", resultado.Titulo);
        Assert.Equal(4, resultado.Stock);
        Assert.Equal("9788420633109", libroExistente.IsbnNormalizado);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLibroAsync_WithoutActiveLoans_DeletesLibro()
    {
        var id = IdsDePrueba.Nuevo();
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
        var id = IdsDePrueba.Nuevo();
        var libro = new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.TieneActivosPorLibroAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroConPrestamosActivosException>(() => service.DeleteAsync(id));
        _libroRepository.Verify(r => r.DeleteAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchLibrosAsync_WithText_ReturnsMatchingLibrosWithDisponibilidad()
    {
        _libroRepository
            .Setup(r => r.GetAllAsync("borges", It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Libro>
            {
                new() { Id = IdsDePrueba.Nuevo(), Titulo = "Ficciones", Autor = "Borges", Isbn = "1", Ubicacion = "A1", Stock = 2 },
                new() { Id = IdsDePrueba.Nuevo(), Titulo = "El Aleph", Autor = "Borges", Isbn = "2", Ubicacion = "A2", Stock = 0 }
            });

        var service = CrearService();
        var resultado = await service.GetAllAsync("borges");

        Assert.Equal(2, resultado.Count);
        Assert.True(resultado[0].Disponible);
        Assert.False(resultado[1].Disponible);
    }

    [Fact]
    public async Task SearchLibrosAsync_WithNoMatches_ReturnsEmptyList()
    {
        _libroRepository
            .Setup(r => r.GetAllAsync("inexistente", It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Libro>());

        var service = CrearService();
        var resultado = await service.GetAllAsync("inexistente");

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task GetLibroDisponibilidadAsync_WithZeroStock_ReturnsUnavailable()
    {
        var id = IdsDePrueba.Nuevo();
        _libroRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Libro { Id = id, Titulo = "Rayuela", Autor = "Cortázar", Isbn = "3", Ubicacion = "B1", Stock = 0 });

        var service = CrearService();
        var resultado = await service.GetByIdAsync(id);

        Assert.Equal(0, resultado.Stock);
        Assert.False(resultado.Disponible);
    }

    private static LibroCreateDTO LibroValidoDto() => new()
    {
        Titulo = "Ficciones",
        Autor = "Jorge Luis Borges",
        Isbn = "978-8420633109",
        Ubicacion = "A1-4",
        Stock = 5
    };

    private void ConfigurarCreateQueDevuelveElLibro() =>
        _libroRepository
            .Setup(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Libro l, CancellationToken _) => l);

    [Fact]
    public async Task CreateLibroAsync_WithStockAboveMaximum_ThrowsStockExcedidoException()
    {
        var dto = LibroValidoDto();
        dto.Stock = 11;

        var service = CrearService();

        await Assert.ThrowsAsync<StockExcedidoException>(() => service.CreateAsync(dto));
        _libroRepository.Verify(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLibroAsync_WithStockAtMaximum_CreatesLibro()
    {
        var dto = LibroValidoDto();
        dto.Stock = 10;
        ConfigurarCreateQueDevuelveElLibro();

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.Equal(10, resultado.Stock);
    }

    [Fact]
    public async Task UpdateLibroAsync_WithStockAboveMaximum_ThrowsStockExcedidoException()
    {
        var id = IdsDePrueba.Nuevo();
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1-1", Stock = 1 });
        var dto = new LibroUpdateDTO { Titulo = "T", Autor = "A", Isbn = "978-8420633109", Ubicacion = "A1-1", Stock = 150000 };

        var service = CrearService();

        await Assert.ThrowsAsync<StockExcedidoException>(() => service.UpdateAsync(id, dto));
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("978842063310")]
    [InlineData("97884206331091")]
    [InlineData("978-84-2063-31A-9")]
    [InlineData("84206331X9")]
    public async Task CreateLibroAsync_WithInvalidIsbn_ThrowsLibroInvalidoException(string isbn)
    {
        var dto = LibroValidoDto();
        dto.Isbn = isbn;

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
    }

    [Theory]
    [InlineData("978-8420633109")]
    [InlineData("9788420633109")]
    [InlineData("84-206-3310-X")]
    [InlineData("8420633109")]
    public async Task CreateLibroAsync_WithValidIsbn_CreatesLibro(string isbn)
    {
        var dto = LibroValidoDto();
        dto.Isbn = isbn;
        ConfigurarCreateQueDevuelveElLibro();

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.Equal(isbn, resultado.Isbn);
    }

    [Theory]
    [InlineData("Estante A4")]
    [InlineData("A1")]
    [InlineData("A14")]
    [InlineData("K1-1")]
    [InlineData("A0-1")]
    [InlineData("A31-1")]
    [InlineData("A1-0")]
    [InlineData("A1-7")]
    [InlineData("A01-1")]
    public async Task CreateLibroAsync_WithInvalidUbicacion_ThrowsLibroInvalidoException(string ubicacion)
    {
        var dto = LibroValidoDto();
        dto.Ubicacion = ubicacion;

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
    }

    [Theory]
    [InlineData("A1-4", "A1-4")]
    [InlineData("J30-6", "J30-6")]
    [InlineData(" c12-3 ", "C12-3")]
    public async Task CreateLibroAsync_WithValidUbicacion_SavesItNormalized(string ubicacion, string esperada)
    {
        var dto = LibroValidoDto();
        dto.Ubicacion = ubicacion;
        ConfigurarCreateQueDevuelveElLibro();

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.Equal(esperada, resultado.Ubicacion);
    }

    [Fact]
    public async Task CreateLibroAsync_WithDuplicateIsbn_ThrowsIsbnDuplicadoException()
    {
        var dto = LibroValidoDto();
        _libroRepository
            .Setup(r => r.ExisteIsbnAsync("9788420633109", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<IsbnDuplicadoException>(() => service.CreateAsync(dto));
        _libroRepository.Verify(r => r.CreateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLibroAsync_WhenIsbnBelongsToOtherLibro_ThrowsIsbnDuplicadoException()
    {
        var id = IdsDePrueba.Nuevo();
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = "8420633109", Ubicacion = "A1-1", Stock = 1 });
        _libroRepository
            .Setup(r => r.ExisteIsbnAsync("9788420633109", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var dto = new LibroUpdateDTO { Titulo = "T", Autor = "A", Isbn = "978-8420633109", Ubicacion = "A1-1", Stock = 1 };

        var service = CrearService();

        await Assert.ThrowsAsync<IsbnDuplicadoException>(() => service.UpdateAsync(id, dto));
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLibroAsync_WithTituloTooLong_ThrowsLibroInvalidoException()
    {
        var dto = LibroValidoDto();
        dto.Titulo = new string('a', 151);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task DeleteLibroAsync_WhenLibroHasActiveReservas_ThrowsConflictException()
    {
        var id = IdsDePrueba.Nuevo();
        var libro = new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = 0 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _reservaRepository.Setup(r => r.TieneActivasPorLibroAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroConReservasActivasException>(() => service.DeleteAsync(id));
        _libroRepository.Verify(r => r.DeleteAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLibroAsync_WhenLibroHasLoanHistory_ThrowsConflictException()
    {
        var id = IdsDePrueba.Nuevo();
        var libro = new Libro { Id = id, Titulo = "T", Autor = "A", Isbn = "1", Ubicacion = "A1", Stock = 1 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        _prestamoRepository.Setup(r => r.TienePrestamosPorLibroAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LibroConHistorialException>(() => service.DeleteAsync(id));
        _libroRepository.Verify(r => r.DeleteAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLibroAsync_WithOnlyTitulo_KeepsTheOtherFields()
    {
        var id = IdsDePrueba.Nuevo();
        var libro = new Libro { Id = id, Titulo = "Ficciones", Autor = "Jorge Luis Borges", Isbn = "978-8420633109", Ubicacion = "A1-4", Stock = 3 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);

        var service = CrearService();
        var resultado = await service.UpdateAsync(id, new LibroUpdateDTO { Titulo = "Ficciones (edición aniversario)" });

        Assert.Equal("Ficciones (edición aniversario)", resultado.Titulo);
        Assert.Equal("Jorge Luis Borges", resultado.Autor);
        Assert.Equal("978-8420633109", resultado.Isbn);
        Assert.Equal("A1-4", resultado.Ubicacion);
        Assert.Equal(3, resultado.Stock);
    }

    [Fact]
    public async Task UpdateLibroAsync_WithEmptyFields_KeepsCurrentValues()
    {
        var id = IdsDePrueba.Nuevo();
        var libro = new Libro { Id = id, Titulo = "Ficciones", Autor = "Borges", Isbn = "978-8420633109", Ubicacion = "A1-4", Stock = 3 };
        _libroRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);

        var service = CrearService();
        var resultado = await service.UpdateAsync(id, new LibroUpdateDTO { Titulo = "   ", Autor = "", Ubicacion = "B2-3" });

        Assert.Equal("Ficciones", resultado.Titulo);
        Assert.Equal("Borges", resultado.Autor);
        Assert.Equal("B2-3", resultado.Ubicacion);
        _libroRepository.Verify(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
    }
}
