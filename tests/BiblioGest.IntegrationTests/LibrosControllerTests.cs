using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Prestamos;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class LibrosControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LibrosControllerTests(CustomWebApplicationFactory factory)
    {
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
}
