using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Usuarios;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class UsuariosControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsuariosControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static UsuarioCreateDTO NuevoUsuarioDto() => new()
    {
        Nombre = "Carla Pérez",
        Email = $"{Guid.NewGuid()}@example.com",
        Password = "secreta1",
        Rol = "Bibliotecario"
    };

    [Fact]
    public async Task GetUsuarios_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/usuarios");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_WithBibliotecarioRole_Returns403Forbidden()
    {
        var bibliotecarioClient = await _factory.CreateBibliotecarioClientAsync();

        var response = await bibliotecarioClient.GetAsync("/api/v1/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUsuario_ReturnsSuccessAndCreatedUsuario()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/usuarios", NuevoUsuarioDto(), CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<UsuarioResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(creado);
        Assert.NotEqual(Guid.Empty, creado!.Id);
        Assert.True(creado.Activo);
    }

    [Fact]
    public async Task CreateUsuario_DoesNotExposePassword()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/usuarios", NuevoUsuarioDto(), CustomWebApplicationFactory.JsonOptions);

        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateUsuario_WhenDuplicateEmail_Returns409Conflict()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoUsuarioDto();
        await client.PostAsJsonAsync("/api/v1/usuarios", dto, CustomWebApplicationFactory.JsonOptions);

        var dtoDuplicado = NuevoUsuarioDto();
        dtoDuplicado.Email = dto.Email;

        var response = await client.PostAsJsonAsync("/api/v1/usuarios", dtoDuplicado, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateUsuario_WithMissingRequiredField_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoUsuarioDto();
        dto.Nombre = "";

        var response = await client.PostAsJsonAsync("/api/v1/usuarios", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUsuario_WithShortPassword_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoUsuarioDto();
        dto.Password = "123";

        var response = await client.PostAsJsonAsync("/api/v1/usuarios", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUsuario_WithInvalidRol_Returns400BadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var dto = NuevoUsuarioDto();
        dto.Rol = "SuperAdmin";

        var response = await client.PostAsJsonAsync("/api/v1/usuarios", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuario_WithUnknownId_Returns404NotFound()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/usuarios/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUsuario_ReturnsSuccessAndUpdatedUsuario()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var creado = await client.PostAsJsonAsync("/api/v1/usuarios", NuevoUsuarioDto(), CustomWebApplicationFactory.JsonOptions);
        var usuario = await creado.Content.ReadFromJsonAsync<UsuarioResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var dto = new UsuarioUpdateDTO
        {
            Nombre = "Nombre actualizado",
            Email = usuario!.Email,
            Rol = "Administrador"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/usuarios/{usuario.Id}", dto, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<UsuarioResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.Equal("Nombre actualizado", actualizado!.Nombre);
        Assert.Equal("Administrador", actualizado.Rol);
    }

    [Fact]
    public async Task DeleteUsuario_ReturnsSuccessAndDeactivatesUsuario()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var creado = await client.PostAsJsonAsync("/api/v1/usuarios", NuevoUsuarioDto(), CustomWebApplicationFactory.JsonOptions);
        var usuario = await creado.Content.ReadFromJsonAsync<UsuarioResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/v1/usuarios/{usuario!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/usuarios/{usuario.Id}");
        var obtenido = await getResponse.Content.ReadFromJsonAsync<UsuarioResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.False(obtenido!.Activo);
    }
}
