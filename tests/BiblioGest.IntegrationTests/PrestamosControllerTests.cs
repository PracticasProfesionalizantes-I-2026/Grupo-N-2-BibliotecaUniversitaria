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

    private async Task<LibroResponseDTO> CrearLibroAsync(int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = Guid.NewGuid().ToString("N")[..10],
            Ubicacion = "Estante Z",
            Stock = stock
        });
        return (await response.Content.ReadFromJsonAsync<LibroResponseDTO>())!;
    }

    private async Task<LectorResponseDTO> CrearLectorAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/lectores", new LectorCreateDTO
        {
            Nombre = "Elena",
            Apellido = "Suárez",
            Email = $"{Guid.NewGuid()}@example.com",
            Identificador = Guid.NewGuid().ToString("N")[..8]
        });
        return (await response.Content.ReadFromJsonAsync<LectorResponseDTO>())!;
    }

    [Fact]
    public async Task CreatePrestamo_ReturnsSuccessAndCreatedPrestamo()
    {
        var libro = await CrearLibroAsync(stock: 2);
        var lector = await CrearLectorAsync();

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal("Activo", creado!.Estado);
    }

    [Fact]
    public async Task CreatePrestamo_WithUnknownLectorOrLibro_Returns404NotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = Guid.NewGuid(),
            LibroId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WithoutStock_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 0);
        var lector = await CrearLectorAsync();

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDevolucion_ReturnsSuccessAndUpdatedPrestamo()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var creadoResponse = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });
        var creado = await creadoResponse.Content.ReadFromJsonAsync<PrestamoResponseDTO>();

        var response = await _client.PutAsync($"/api/prestamos/{creado!.Id}/devolucion", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        Assert.Equal("Devuelto", actualizado!.Estado);
        Assert.NotNull(actualizado.FechaDevolucion);
    }

    [Fact]
    public async Task GetPrestamosVencidos_ReturnsSuccessAndOverdueList()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();

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

        var response = await _client.GetAsync("/api/prestamos/mora");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var vencidos = await response.Content.ReadFromJsonAsync<List<PrestamoResponseDTO>>();
        Assert.NotNull(vencidos);
        Assert.Contains(vencidos!, p => p.LibroId == libro.Id && p.EnMora);
    }
}
