using System.Net;
using System.Net.Http.Json;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Prestamos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class PrestamosControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PrestamosControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static async Task<LibroResponseDTO> CrearLibroAsync(HttpClient client, int stock)
    {
        var response = await client.PostAsJsonAsync("/api/v1/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = Guid.NewGuid().ToString("N")[..10],
            Ubicacion = "Estante Z",
            Stock = stock
        }, CustomWebApplicationFactory.JsonOptions);
        return (await response.Content.ReadFromJsonAsync<LibroResponseDTO>(CustomWebApplicationFactory.JsonOptions))!;
    }

    private static async Task<LectorResponseDTO> CrearLectorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/lectores", new LectorCreateDTO
        {
            Nombre = "Elena",
            Apellido = "Suárez",
            Email = $"{Guid.NewGuid()}@example.com",
            Identificador = Guid.NewGuid().ToString("N")[..8]
        }, CustomWebApplicationFactory.JsonOptions);
        return (await response.Content.ReadFromJsonAsync<LectorResponseDTO>(CustomWebApplicationFactory.JsonOptions))!;
    }

    [Fact]
    public async Task GetPrestamosMora_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/prestamos/mora");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_ReturnsSuccessAndCreatedPrestamo()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var libro = await CrearLibroAsync(client, stock: 2);
        var lector = await CrearLectorAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<PrestamoResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(creado);
        Assert.Equal("Activo", creado!.Estado);
    }

    [Fact]
    public async Task CreatePrestamo_WithUnknownLectorOrLibro_Returns404NotFound()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = Guid.NewGuid(),
            LibroId = Guid.NewGuid()
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WhenLectorHasThreeActiveLoans_Returns409Conflict()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var lector = await CrearLectorAsync(client);

        for (var i = 0; i < 3; i++)
        {
            var libro = await CrearLibroAsync(client, stock: 1);
            var response = await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
            {
                LectorId = lector.Id,
                LibroId = libro.Id
            }, CustomWebApplicationFactory.JsonOptions);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var libroExtra = await CrearLibroAsync(client, stock: 1);
        var responseExtra = await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libroExtra.Id
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, responseExtra.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WhenLectorInMora_Returns409Conflict()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var lector = await CrearLectorAsync(client);
        var libroVencido = await CrearLibroAsync(client, stock: 1);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
            context.Prestamos.Add(new Prestamo
            {
                Id = Guid.NewGuid(),
                LectorId = lector.Id,
                LibroId = libroVencido.Id,
                FechaPrestamo = DateTime.UtcNow.AddDays(-20),
                FechaVencimiento = DateTime.UtcNow.AddDays(-6),
                Estado = EstadoPrestamo.Activo
            });
            await context.SaveChangesAsync();
        }

        var libroNuevo = await CrearLibroAsync(client, stock: 1);
        var response = await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libroNuevo.Id
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WithoutStock_Returns409Conflict()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var libro = await CrearLibroAsync(client, stock: 0);
        var lector = await CrearLectorAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDevolucion_ReturnsSuccessAndUpdatedPrestamo()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var libro = await CrearLibroAsync(client, stock: 1);
        var lector = await CrearLectorAsync(client);
        var creadoResponse = await client.PostAsJsonAsync("/api/v1/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        }, CustomWebApplicationFactory.JsonOptions);
        var creado = await creadoResponse.Content.ReadFromJsonAsync<PrestamoResponseDTO>(CustomWebApplicationFactory.JsonOptions);

        var response = await client.PutAsync($"/api/v1/prestamos/{creado!.Id}/devolucion", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<PrestamoResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.Equal("Devuelto", actualizado!.Estado);
        Assert.NotNull(actualizado.FechaDevolucion);
    }

    [Fact]
    public async Task GetPrestamosVencidos_ReturnsSuccessAndOverdueList()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var libro = await CrearLibroAsync(client, stock: 1);
        var lector = await CrearLectorAsync(client);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
            context.Prestamos.Add(new Prestamo
            {
                Id = Guid.NewGuid(),
                LectorId = lector.Id,
                LibroId = libro.Id,
                FechaPrestamo = DateTime.UtcNow.AddDays(-20),
                FechaVencimiento = DateTime.UtcNow.AddDays(-6),
                Estado = EstadoPrestamo.Activo
            });
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/prestamos/mora");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var vencidos = await response.Content.ReadFromJsonAsync<List<PrestamoResponseDTO>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(vencidos);
        Assert.Contains(vencidos!, p => p.LibroId == libro.Id && p.EnMora);
    }

    [Fact]
    public async Task GetPrestamosVencidos_WithNoOverdueLoans_Returns200OKWithEmptyList()
    {
        // Usa una factory propia (base en memoria completamente nueva) en vez de la
        // compartida por la clase, para garantizar que no haya vencidos cargados por
        // otros tests. El DbInitializer siembra un préstamo vencido de prueba, así
        // que primero se registra su devolución para dejar el sistema sin mora.
        using var factory = new CustomWebApplicationFactory();
        var client = await factory.CreateAuthenticatedClientAsync();

        var vencidosIniciales = await client.GetFromJsonAsync<List<PrestamoResponseDTO>>(
            "/api/v1/prestamos/mora", CustomWebApplicationFactory.JsonOptions);
        foreach (var vencido in vencidosIniciales!)
        {
            await client.PutAsync($"/api/v1/prestamos/{vencido.Id}/devolucion", content: null);
        }

        var response = await client.GetAsync("/api/v1/prestamos/mora");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var vencidos = await response.Content.ReadFromJsonAsync<List<PrestamoResponseDTO>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(vencidos);
        Assert.Empty(vencidos!);
    }
}
