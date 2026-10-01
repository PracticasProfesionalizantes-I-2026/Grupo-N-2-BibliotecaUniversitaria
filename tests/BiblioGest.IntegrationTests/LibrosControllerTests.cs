using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Prestamos;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class LibrosControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LibrosControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static LibroCreateDTO NuevoLibroDto(int stock = 3) => new()
    {
        Titulo = $"Libro {Guid.NewGuid()}",
        Autor = "Autor de Prueba",
        Isbn = Guid.NewGuid().ToString("N")[..10],
        Ubicacion = "Estante Z",
        Stock = stock
    };

    [Fact]
    public async Task GetLibros_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateLibro_ReturnsSuccessAndCreatedLibro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/libros", NuevoLibroDto(), CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<LibroResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(creado);
        Assert.NotEqual(Guid.Empty, creado!.Id);
    }

    [Fact]
    public async Task CreateLibro_WithMissingRequiredField_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoLibroDto();
        dto.Titulo = "";

        var response = await client.PostAsJsonAsync("/api/v1/libros", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLibro_ReturnsSuccessAndUpdatedLibro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var creado = await client.PostAsJsonAsync("/api/v1/libros", NuevoLibroDto(), CustomWebApplicationFactory.JsonOptions);
        var libro = await creado.Content.ReadFromJsonAsync<LibroResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var dto = new LibroUpdateDTO
        {
            Titulo = "Título actualizado",
            Autor = libro!.Autor,
            Isbn = libro.Isbn,
            Ubicacion = libro.Ubicacion,
            Stock = libro.Stock + 1
        };

        var response = await client.PutAsJsonAsync($"/api/v1/libros/{libro.Id}", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<LibroResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.Equal("Título actualizado", actualizado!.Titulo);
        Assert.Equal(libro.Stock + 1, actualizado.Stock);
    }

    [Fact]
    public async Task DeleteLibro_WithoutActiveLoans_Returns200OK()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var creado = await client.PostAsJsonAsync("/api/v1/libros", NuevoLibroDto(), CustomWebApplicationFactory.JsonOptions);
        var libro = await creado.Content.ReadFromJsonAsync<LibroResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var response = await client.DeleteAsync($"/api/v1/libros/{libro!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateLibro_WithNegativeStock_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoLibroDto(stock: -1);

        var response = await client.PostAsJsonAsync("/api/v1/libros", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLibro_WithTituloTooLong_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoLibroDto();
        dto.Titulo = new string('a', 201);

        var response = await client.PostAsJsonAsync("/api/v1/libros", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLibro_WithUnknownId_Returns404NotFound()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/libros/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLibro_WithActiveLoans_Returns409Conflict()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var libroResponse = await client.PostAsJsonAsync("/api/v1/libros", NuevoLibroDto(stock: 1), CustomWebApplicationFactory.JsonOptions);
        var libro = await libroResponse.Content.ReadFromJsonAsync<LibroResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var lectorDto = new LectorCreateDTO
        {
            Nombre = "Carla",
            Apellido = "Ruiz",
            Email = "carla.ruiz@example.com",
            Identificador = Guid.NewGuid().ToString("N")[..8]
        };
        var lectorResponse = await client.PostAsJsonAsync("/api/v1/lectores", lectorDto, CustomWebApplicationFactory.JsonOptions);
        var lector = await lectorResponse.Content.ReadFromJsonAsync<LectorResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector!.Id,
            LibroId = libro!.Id
        }, CustomWebApplicationFactory.JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/v1/libros/{libro.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }
}
