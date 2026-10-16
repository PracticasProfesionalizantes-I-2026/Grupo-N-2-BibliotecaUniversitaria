using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Usuarios;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class UsuariosControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public UsuariosControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static UsuarioCreateDTO NuevoUsuarioDto() => new()
    {
        Email = $"{Guid.NewGuid()}@bibliogest.com",
        Password = "Clave123",
        RolUsuario = "Bibliotecario"
    };

    [Fact]
    public async Task CreateUsuario_ReturnsSuccessAndCreatedUsuario()
    {
        var dto = NuevoUsuarioDto();

        var response = await _client.PostAsJsonAsync("/api/usuarios", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<UsuarioResponseDTO>();
        Assert.NotNull(creado);
        Assert.NotEqual(Guid.Empty, creado!.Id);
        Assert.Equal(dto.Email, creado.Email);
    }

    [Fact]
    public async Task CreateUsuario_WithMissingRequiredField_Returns400BadRequest()
    {
        var dto = NuevoUsuarioDto();
        dto.Password = "123";

        var response = await _client.PostAsJsonAsync("/api/usuarios", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUsuario_WhenDuplicateEmail_Returns409Conflict()
    {
        var dto = NuevoUsuarioDto();
        await _client.PostAsJsonAsync("/api/usuarios", dto);

        var response = await _client.PostAsJsonAsync("/api/usuarios", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUsuario_ReturnsSuccessAndUpdatedUsuario()
    {
        var crearResponse = await _client.PostAsJsonAsync("/api/usuarios", NuevoUsuarioDto());
        var creado = await crearResponse.Content.ReadFromJsonAsync<UsuarioResponseDTO>();
        var dto = new UsuarioUpdateDTO
        {
            Email = $"{Guid.NewGuid()}@bibliogest.com",
            RolUsuario = "Administrador"
        };

        var response = await _client.PutAsJsonAsync($"/api/usuarios/{creado!.Id}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<UsuarioResponseDTO>();
        Assert.Equal(dto.Email, actualizado!.Email);
        Assert.Equal("Administrador", actualizado.RolUsuario);
    }

    [Fact]
    public async Task DeleteUsuario_ReturnsSuccessAndDeletesUsuario()
    {
        var crearResponse = await _client.PostAsJsonAsync("/api/usuarios", NuevoUsuarioDto());
        var creado = await crearResponse.Content.ReadFromJsonAsync<UsuarioResponseDTO>();

        var response = await _client.DeleteAsync($"/api/usuarios/{creado!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var usuario = await _client.GetFromJsonAsync<UsuarioResponseDTO>($"/api/usuarios/{creado.Id}");
        Assert.False(usuario!.Activo);
    }

    [Fact]
    public async Task GetUsuario_WithUnknownId_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/usuarios/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_AsBibliotecario_Returns403Forbidden()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("bibliotecario@bibliogest.com", "Bibliotecario123!");

        var response = await client.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
