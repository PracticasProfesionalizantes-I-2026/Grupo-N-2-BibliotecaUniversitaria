using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Prestamos;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class LectoresControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public LectoresControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static LectorCreateDTO NuevoLectorDto() => new()
    {
        Nombre = "Diego",
        Apellido = "Fernández",
        Email = $"{Guid.NewGuid()}@example.com",
        Identificador = Guid.NewGuid().ToString("N")[..8]
    };

    [Fact]
    public async Task CreateLector_ReturnsSuccessAndCreatedLector()
    {
        var response = await _client.PostAsJsonAsync("/api/lectores", NuevoLectorDto());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<LectorResponseDTO>();
        Assert.NotNull(creado);
        Assert.NotEqual(Guid.Empty, creado!.Id);
    }

    [Fact]
    public async Task CreateLector_WhenDuplicateIdentificador_Returns409Conflict()
    {
        var dto = NuevoLectorDto();
        await _client.PostAsJsonAsync("/api/lectores", dto);

        var dtoDuplicado = NuevoLectorDto();
        dtoDuplicado.Identificador = dto.Identificador;

        var response = await _client.PostAsJsonAsync("/api/lectores", dtoDuplicado);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetLector_WithUnknownId_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/lectores/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateLector_WithMissingRequiredField_Returns400BadRequest()
    {
        var dto = NuevoLectorDto();
        dto.Nombre = "";

        var response = await _client.PostAsJsonAsync("/api/lectores", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLector_ReturnsSuccessAndUpdatedLector()
    {
        var crearResponse = await _client.PostAsJsonAsync("/api/lectores", NuevoLectorDto());
        var creado = await crearResponse.Content.ReadFromJsonAsync<LectorResponseDTO>();
        var dto = new LectorUpdateDTO
        {
            Nombre = "Diego Martín",
            Apellido = creado!.Apellido,
            Email = $"{Guid.NewGuid()}@example.com",
            Identificador = creado.Identificador
        };

        var response = await _client.PutAsJsonAsync($"/api/lectores/{creado.Id}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<LectorResponseDTO>();
        Assert.Equal("Diego Martín", actualizado!.Nombre);
    }

    [Fact]
    public async Task DeleteLector_WithoutActiveLoans_Returns200OK()
    {
        var crearResponse = await _client.PostAsJsonAsync("/api/lectores", NuevoLectorDto());
        var creado = await crearResponse.Content.ReadFromJsonAsync<LectorResponseDTO>();

        var response = await _client.DeleteAsync($"/api/lectores/{creado!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var getResponse = await _client.GetAsync($"/api/lectores/{creado.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteLector_WithActiveLoans_Returns409Conflict()
    {
        var lectorResponse = await _client.PostAsJsonAsync("/api/lectores", NuevoLectorDto());
        var lector = await lectorResponse.Content.ReadFromJsonAsync<LectorResponseDTO>();
        var libroResponse = await _client.PostAsJsonAsync("/api/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = Guid.NewGuid().ToString("N")[..10],
            Ubicacion = "Estante Z",
            Stock = 1
        });
        var libro = await libroResponse.Content.ReadFromJsonAsync<LibroResponseDTO>();
        await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector!.Id,
            LibroId = libro!.Id
        });

        var response = await _client.DeleteAsync($"/api/lectores/{lector.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
