using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Lectores;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class LectoresControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LectoresControllerTests(CustomWebApplicationFactory factory)
    {
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
}
