using System.Net;
using System.Net.Http.Json;
using System.Text;
using BiblioGest.DataAccess;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.DTOs.Usuarios;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class ReglasAdicionalesTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ReglasAdicionalesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record MensajeDeError(string Message);

    private static LectorCreateDTO NuevoLectorDto() => new()
    {
        Nombre = "Tomás",
        Apellido = "Ríos",
        Email = $"{Guid.NewGuid()}@example.com",
        Dni = Random.Shared.Next(10_000_000, 100_000_000).ToString()
    };

    private async Task<LibroResponseDTO> CrearLibroAsync(int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = $"978{Random.Shared.NextInt64(1_000_000_000, 10_000_000_000)}",
            Ubicacion = "C3-2",
            Stock = stock
        });
        return (await response.Content.ReadFromJsonAsync<LibroResponseDTO>())!;
    }

    private async Task<LectorResponseDTO> CrearLectorAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/lectores", NuevoLectorDto());
        return (await response.Content.ReadFromJsonAsync<LectorResponseDTO>())!;
    }

    private int IdDelAdministradorSeed()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
        return context.Usuarios.Single(u => u.Email == "admin@bibliogest.com").Id;
    }

    [Fact]
    public async Task CreateLector_WithEmailOfAnotherLectorIgnoringCase_Returns409Conflict()
    {
        var existente = NuevoLectorDto();
        await _client.PostAsJsonAsync("/api/lectores", existente);
        var repetido = NuevoLectorDto();
        repetido.Email = existente.Email.ToUpperInvariant();

        var response = await _client.PostAsJsonAsync("/api/lectores", repetido);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WhenLectorAlreadyHasSameBookActive_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 2);
        var lector = await CrearLectorAsync();
        var dto = new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id };
        await _client.PostAsJsonAsync("/api/prestamos", dto);

        var response = await _client.PostAsJsonAsync("/api/prestamos", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetEstadoLector_WithFreeLector_ReturnsCanBorrow()
    {
        var lector = await CrearLectorAsync();

        var estado = await _client.GetFromJsonAsync<EstadoLectorPrestamoDTO>($"/api/prestamos/estado-lector/{lector.Id}");

        Assert.True(estado!.PuedePedir);
        Assert.Equal(0, estado.PrestamosActivos);
        Assert.Null(estado.Motivo);
    }

    [Fact]
    public async Task GetEstadoLector_WithUnknownLector_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/prestamos/estado-lector/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPrestamosActivos_ReturnsOnlyActiveLoans()
    {
        var libro = await CrearLibroAsync(stock: 2);
        var lector = await CrearLectorAsync();
        var activo = await (await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id }))
            .Content.ReadFromJsonAsync<PrestamoResponseDTO>();

        var activos = await _client.GetFromJsonAsync<List<PrestamoResponseDTO>>("/api/prestamos/activos");

        Assert.Contains(activos!, p => p.Id == activo!.Id);
        Assert.All(activos!, p => Assert.Equal("Activo", p.Estado));
    }

    [Fact]
    public async Task DeleteUsuario_WhenDeletingOwnAccount_Returns409Conflict()
    {
        var response = await _client.DeleteAsync($"/api/usuarios/{IdDelAdministradorSeed()}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUsuario_WhenRemovingOwnAdministratorRole_Returns409Conflict()
    {
        var response = await _client.PutAsJsonAsync($"/api/usuarios/{IdDelAdministradorSeed()}", new UsuarioUpdateDTO
        {
            Email = "admin@bibliogest.com",
            RolUsuario = "Bibliotecario"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithoutToken_Returns401WithMessageInSpanish()
    {
        var response = await _factory.CreateClient().GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<MensajeDeError>();
        Assert.Equal("Debe iniciar sesión.", error!.Message);
    }

    [Fact]
    public async Task Request_WithInvalidToken_Returns401WithMessageInSpanish()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token-invalido");

        var response = await client.GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<MensajeDeError>();
        Assert.Equal("Token inválido o vencido.", error!.Message);
    }

    [Fact]
    public async Task Request_WithInsufficientRole_Returns403WithMessageInSpanish()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("bibliotecario@bibliogest.com", "Bibliotecario123!");

        var response = await client.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<MensajeDeError>();
        Assert.Equal("No tiene permisos para realizar esta acción.", error!.Message);
    }

    [Fact]
    public async Task Request_WithMalformedJson_Returns400WithMessageInSpanish()
    {
        var contenido = new StringContent("{ \"nombre\": ", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/lectores", contenido);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<MensajeDeError>();
        Assert.Equal("El cuerpo de la solicitud no es un JSON válido.", error!.Message);
    }
}
