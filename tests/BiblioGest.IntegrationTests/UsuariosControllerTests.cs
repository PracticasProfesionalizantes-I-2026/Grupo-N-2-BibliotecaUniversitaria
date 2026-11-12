using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BiblioGest.Shared.DTOs.Usuarios;
using Microsoft.IdentityModel.Tokens;
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
        Assert.True(creado!.Id > 0);
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
        var response = await _client.GetAsync($"/api/usuarios/{int.MaxValue}");

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

    [Fact]
    public async Task UpdateUsuario_WithTokenMissingNameIdentifierClaim_Returns401Unauthorized()
    {
        var usuario = await (await _client.PostAsJsonAsync("/api/usuarios", NuevoUsuarioDto()))
            .Content.ReadFromJsonAsync<UsuarioResponseDTO>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CrearTokenSinClaimDeId());

        var response = await client.PutAsJsonAsync($"/api/usuarios/{usuario!.Id}", new UsuarioUpdateDTO
        {
            Email = usuario.Email,
            RolUsuario = "Bibliotecario"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUsuario_WithTokenMissingNameIdentifierClaim_Returns401Unauthorized()
    {
        var usuario = await (await _client.PostAsJsonAsync("/api/usuarios", NuevoUsuarioDto()))
            .Content.ReadFromJsonAsync<UsuarioResponseDTO>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CrearTokenSinClaimDeId());

        var response = await client.DeleteAsync($"/api/usuarios/{usuario!.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Token válido (misma clave/issuer/audience que appsettings.json) y con
    // el rol correcto, pero sin el claim de identificador de usuario: simula
    // un token mal emitido para probar que UsuarioActualId() no tira 500.
    private static string CrearTokenSinClaimDeId()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "sin-id@bibliogest.com"),
            new(ClaimTypes.Role, "Administrador")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("UnaClaveSecretaBienLargaYDificilDeAdivinar123!"));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "BiblioGestApi",
            audience: "BiblioGestClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("juan perez@bibliogest.com")]
    [InlineData("a@b.c")]
    public async Task CreateUsuario_WithInvalidEmail_Returns400BadRequest(string email)
    {
        var dto = NuevoUsuarioDto();
        dto.Email = email;

        var response = await _client.PostAsJsonAsync("/api/usuarios", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUsuario_WithPasswordTooLong_Returns400BadRequest()
    {
        var dto = NuevoUsuarioDto();
        dto.Password = new string('a', 65);

        var response = await _client.PostAsJsonAsync("/api/usuarios", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
