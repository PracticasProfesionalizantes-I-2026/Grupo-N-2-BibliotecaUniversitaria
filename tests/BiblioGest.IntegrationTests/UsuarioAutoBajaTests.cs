using System.Net;
using System.Net.Http.Json;
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
}
