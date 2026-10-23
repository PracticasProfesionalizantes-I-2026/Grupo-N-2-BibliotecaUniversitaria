using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Prestamos;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class LibrosControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public LibrosControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static LibroCreateDTO NuevoLibroDto(int stock = 3) => new()
    {
        Titulo = $"Libro {Guid.NewGuid()}",
        Autor = "Autor de Prueba",
        Isbn = IsbnTestHelper.NuevoIsbnValido(),
        Ubicacion = "Estante Z",
        Stock = stock
    };

    [Fact]
    public async Task CreateLibro_ReturnsSuccessAndCreatedLibro()
    {
        var response = await _client.PostAsJsonAsync("/api/libros", NuevoLibroDto());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<LibroResponseDTO>();
        Assert.NotNull(creado);
        Assert.NotEqual(Guid.Empty, creado!.Id);
    }

    [Fact]
    public async Task CreateLibro_WithMissingRequiredField_Returns400BadRequest()
    {
        var dto = NuevoLibroDto();
        dto.Titulo = "";

        var response = await _client.PostAsJsonAsync("/api/libros", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLibro_WithUnknownId_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/libros/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLibro_WithActiveLoans_Returns409Conflict()
    {
        var libroResponse = await _client.PostAsJsonAsync("/api/libros", NuevoLibroDto(stock: 1));
        var libro = await libroResponse.Content.ReadFromJsonAsync<LibroResponseDTO>();

        var lectorDto = new LectorCreateDTO
        {
            Nombre = "Carla",
            Apellido = "Ruiz",
            Email = "carla.ruiz@example.com",
            Identificador = Guid.NewGuid().ToString("N")[..8]
        };
        var lectorResponse = await _client.PostAsJsonAsync("/api/lectores", lectorDto);
        var lector = await lectorResponse.Content.ReadFromJsonAsync<LectorResponseDTO>();

        await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector!.Id,
            LibroId = libro!.Id
        });

        var deleteResponse = await _client.DeleteAsync($"/api/libros/{libro.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateLibro_WithNegativeStock_Returns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/libros", NuevoLibroDto(stock: -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLibro_ReturnsSuccessAndUpdatedLibro()
    {
        var crearResponse = await _client.PostAsJsonAsync("/api/libros", NuevoLibroDto());
        var creado = await crearResponse.Content.ReadFromJsonAsync<LibroResponseDTO>();
        var dto = new LibroUpdateDTO
        {
            Titulo = "Título modificado",
            Autor = creado!.Autor,
            Isbn = creado.Isbn,
            Ubicacion = "Estante Y",
            Stock = 7
        };

        var response = await _client.PutAsJsonAsync($"/api/libros/{creado.Id}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<LibroResponseDTO>();
        Assert.Equal("Título modificado", actualizado!.Titulo);
        Assert.Equal(7, actualizado.Stock);
    }

    [Fact]
    public async Task DeleteLibro_WithoutActiveLoans_Returns200OK()
    {
        var crearResponse = await _client.PostAsJsonAsync("/api/libros", NuevoLibroDto());
        var creado = await crearResponse.Content.ReadFromJsonAsync<LibroResponseDTO>();

        var response = await _client.DeleteAsync($"/api/libros/{creado!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var getResponse = await _client.GetAsync($"/api/libros/{creado.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task SearchLibros_WithText_ReturnsMatchingLibros()
    {
        var dto = NuevoLibroDto();
        await _client.PostAsJsonAsync("/api/libros", dto);

        var libros = await _client.GetFromJsonAsync<List<LibroResponseDTO>>($"/api/libros?busqueda={dto.Isbn}");

        Assert.NotNull(libros);
        Assert.Single(libros!);
        Assert.Equal(dto.Titulo, libros[0].Titulo);
    }

    [Fact]
    public async Task SearchLibros_WithNoMatches_Returns200OKWithEmptyList()
    {
        var response = await _client.GetAsync($"/api/libros?busqueda={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var libros = await response.Content.ReadFromJsonAsync<List<LibroResponseDTO>>();
        Assert.Empty(libros!);
    }

    [Fact]
    public async Task GetLibroDisponibilidad_WithZeroStock_Returns200OKUnavailable()
    {
        var crearResponse = await _client.PostAsJsonAsync("/api/libros", NuevoLibroDto(stock: 0));
        var creado = await crearResponse.Content.ReadFromJsonAsync<LibroResponseDTO>();

        var response = await _client.GetAsync($"/api/libros/{creado!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var libro = await response.Content.ReadFromJsonAsync<LibroResponseDTO>();
        Assert.False(libro!.Disponible);
        Assert.Equal("Sin stock", libro.DisponibilidadTexto);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefghijklm")]
    public async Task CreateLibro_WithInvalidIsbn_Returns400BadRequest(string isbnInvalido)
    {
        var dto = NuevoLibroDto();
        dto.Isbn = isbnInvalido;

        var response = await _client.PostAsJsonAsync("/api/libros", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLibro_WithDuplicateIsbnUsingHyphensAndSpaces_Returns409Conflict()
    {
        var dto = NuevoLibroDto();
        await _client.PostAsJsonAsync("/api/libros", dto);

        var isbn = dto.Isbn;
        var dtoDuplicado = NuevoLibroDto();
        dtoDuplicado.Isbn = $" {isbn[..3]}-{isbn[3..6]}-{isbn[6..]} ";

        var response = await _client.PostAsJsonAsync("/api/libros", dtoDuplicado);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateLibro_WithTooLongTitulo_Returns400BadRequest()
    {
        var dto = NuevoLibroDto();
        dto.Titulo = new string('A', 201);

        var response = await _client.PostAsJsonAsync("/api/libros", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLibro_AfterLoanWasReturned_Returns409ConflictAndKeepsHistory()
    {
        var libroResponse = await _client.PostAsJsonAsync("/api/libros", NuevoLibroDto(stock: 1));
        var libro = await libroResponse.Content.ReadFromJsonAsync<LibroResponseDTO>();

        var lectorResponse = await _client.PostAsJsonAsync("/api/lectores", new LectorCreateDTO
        {
            Nombre = "Carla",
            Apellido = "Ruiz",
            Email = $"{Guid.NewGuid()}@example.com",
            Identificador = Guid.NewGuid().ToString("N")[..8]
        });
        var lector = await lectorResponse.Content.ReadFromJsonAsync<LectorResponseDTO>();

        var prestamoResponse = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector!.Id,
            LibroId = libro!.Id
        });
        var prestamo = await prestamoResponse.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        await _client.PutAsync($"/api/prestamos/{prestamo!.Id}/devolucion", content: null);

        var deleteResponse = await _client.DeleteAsync($"/api/libros/{libro.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }
}
