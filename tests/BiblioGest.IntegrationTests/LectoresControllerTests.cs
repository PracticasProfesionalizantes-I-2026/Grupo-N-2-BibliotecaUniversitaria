using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Prestamos;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class LectoresControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LectoresControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static LectorCreateDTO NuevoLectorDto() => new()
    {
        Nombre = "Diego",
        Apellido = "Fernández",
        Email = $"{Guid.NewGuid()}@example.com",
        Identificador = Guid.NewGuid().ToString("N")[..8]
    };

    [Fact]
    public async Task GetLectores_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/lectores");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateLector_ReturnsSuccessAndCreatedLector()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/lectores", NuevoLectorDto(), CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<LectorResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(creado);
        Assert.NotEqual(Guid.Empty, creado!.Id);
    }

    [Fact]
    public async Task CreateLector_WhenDuplicateIdentificador_Returns409Conflict()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoLectorDto();
        await client.PostAsJsonAsync("/api/v1/lectores", dto, CustomWebApplicationFactory.JsonOptions);

        var dtoDuplicado = NuevoLectorDto();
        dtoDuplicado.Identificador = dto.Identificador;

        var response = await client.PostAsJsonAsync("/api/v1/lectores", dtoDuplicado, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetLector_WithUnknownId_Returns404NotFound()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/lectores/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLector_ReturnsSuccessAndUpdatedLector()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var creado = await client.PostAsJsonAsync("/api/v1/lectores", NuevoLectorDto(), CustomWebApplicationFactory.JsonOptions);
        var lector = await creado.Content.ReadFromJsonAsync<LectorResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var dto = new LectorUpdateDTO
        {
            Nombre = "Nombre actualizado",
            Apellido = lector!.Apellido,
            Email = lector.Email,
            Identificador = lector.Identificador
        };

        var response = await client.PutAsJsonAsync($"/api/v1/lectores/{lector.Id}", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<LectorResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.Equal("Nombre actualizado", actualizado!.Nombre);
    }

    [Fact]
    public async Task DeleteLector_WithoutActiveLoans_Returns200OK()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var creado = await client.PostAsJsonAsync("/api/v1/lectores", NuevoLectorDto(), CustomWebApplicationFactory.JsonOptions);
        var lector = await creado.Content.ReadFromJsonAsync<LectorResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var response = await client.DeleteAsync($"/api/v1/lectores/{lector!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLector_WithActiveLoans_Returns409Conflict()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var lectorResponse = await client.PostAsJsonAsync("/api/v1/lectores", NuevoLectorDto(), CustomWebApplicationFactory.JsonOptions);
        var lector = await lectorResponse.Content.ReadFromJsonAsync<LectorResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var libroDto = new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = Guid.NewGuid().ToString("N")[..10],
            Ubicacion = "Estante Z",
            Stock = 1
        };
        var libroResponse = await client.PostAsJsonAsync("/api/v1/libros", libroDto, CustomWebApplicationFactory.JsonOptions);
        var libro = await libroResponse.Content.ReadFromJsonAsync<LibroResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector!.Id,
            LibroId = libro!.Id
        }, CustomWebApplicationFactory.JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/v1/lectores/{lector.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateLector_WithMissingRequiredField_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoLectorDto();
        dto.Nombre = "";

        var response = await client.PostAsJsonAsync("/api/v1/lectores", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLector_WithInvalidEmail_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoLectorDto();
        dto.Email = "no-es-un-email";

        var response = await client.PostAsJsonAsync("/api/v1/lectores", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
