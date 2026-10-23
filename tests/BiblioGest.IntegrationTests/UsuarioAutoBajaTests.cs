using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.DTOs.Usuarios;
using Xunit;

namespace BiblioGest.IntegrationTests;

// Aislado en su propia fábrica (no comparte estado con UsuariosControllerTests)
// porque desactiva/elimina al administrador autenticado.
public class UsuarioAutoBajaTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public UsuarioAutoBajaTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => _client = await _factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> ObtenerIdDelAdminLogueadoAsync()
    {
        var usuarios = await _client.GetFromJsonAsync<List<UsuarioResponseDTO>>("/api/usuarios");
        return usuarios!.Single(u => u.Email == "admin@bibliogest.com").Id;
    }

    [Fact]
    public async Task DeleteUsuario_WhenAdminDeletesHimself_Returns409Conflict()
    {
        var id = await ObtenerIdDelAdminLogueadoAsync();

        var response = await _client.DeleteAsync($"/api/usuarios/{id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUsuario_WhenAdminDeactivatesHimself_Returns409Conflict()
    {
        var id = await ObtenerIdDelAdminLogueadoAsync();

        var response = await _client.PutAsJsonAsync($"/api/usuarios/{id}", new UsuarioUpdateDTO
        {
            Email = "admin@bibliogest.com",
            Activo = false
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUsuario_WhenLastActiveAdminDeactivatedByDifferentStaleToken_Returns409Conflict()
    {
        // Crea un segundo admin y guarda su token (2 admins activos: permitido).
        var crearAdminB = await _client.PostAsJsonAsync("/api/usuarios", new UsuarioCreateDTO
        {
            Email = $"{Guid.NewGuid()}@bibliogest.com",
            Password = "Clave123",
            RolUsuario = "Administrador"
        });
        var adminB = await crearAdminB.Content.ReadFromJsonAsync<UsuarioResponseDTO>();

        var loginB = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = adminB!.Email,
            Password = "Clave123"
        });
        var tokenB = (await loginB.Content.ReadFromJsonAsync<LoginResponseDTO>())!.Token;

        // El admin original (seed) desactiva a B: todavia hay 2 admins activos, permitido.
        var desactivarB = await _client.PutAsJsonAsync($"/api/usuarios/{adminB.Id}", new UsuarioUpdateDTO
        {
            Email = adminB.Email,
            Activo = false
        });
        Assert.Equal(HttpStatusCode.OK, desactivarB.StatusCode);

        var adminSeedId = await ObtenerIdDelAdminLogueadoAsync();

        // B, ya desactivado pero con un token todavia valido (emitido antes),
        // intenta desactivar al unico admin activo que queda.
        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await clientB.PutAsJsonAsync($"/api/usuarios/{adminSeedId}", new UsuarioUpdateDTO
        {
            Email = "admin@bibliogest.com",
            Activo = false
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
