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

public class PrestamosControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PrestamosControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<LibroResponseDTO> CrearLibroAsync(int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = $"978{Random.Shared.NextInt64(1_000_000_000, 10_000_000_000)}",
            Ubicacion = "J30-6",
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
            Dni = Random.Shared.Next(10_000_000, 100_000_000).ToString()
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
    public async Task GetPrestamosActivos_WithPagination_ReturnsNonOverlappingPages()
    {
        var libro = await CrearLibroAsync(stock: 2);
        var lector1 = await CrearLectorAsync();
        var lector2 = await CrearLectorAsync();
        await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lector1.Id, LibroId = libro.Id });
        await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lector2.Id, LibroId = libro.Id });

        var pagina1 = await _client.GetFromJsonAsync<List<PrestamoResponseDTO>>("/api/prestamos/activos?pageNumber=1&pageSize=1");
        var pagina2 = await _client.GetFromJsonAsync<List<PrestamoResponseDTO>>("/api/prestamos/activos?pageNumber=2&pageSize=1");

        Assert.Single(pagina1!);
        Assert.Single(pagina2!);
        Assert.NotEqual(pagina1![0].Id, pagina2![0].Id);
    }

    [Fact]
    public async Task CreatePrestamo_WithUnknownLectorOrLibro_Returns404NotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = int.MaxValue,
            LibroId = int.MaxValue
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
    public async Task RegisterDevolucion_WhenAlreadyReturned_Returns409ConflictAndKeepsStock()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var creadoResponse = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });
        var creado = await creadoResponse.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        await _client.PutAsync($"/api/prestamos/{creado!.Id}/devolucion", content: null);

        var response = await _client.PutAsync($"/api/prestamos/{creado.Id}/devolucion", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var libroActual = await _client.GetFromJsonAsync<LibroResponseDTO>($"/api/libros/{libro.Id}");
        Assert.Equal(1, libroActual!.Stock);
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

    [Fact]
    public async Task CreatePrestamo_WhenLectorHasThreeActiveLoans_Returns403Forbidden()
    {
        var lector = await CrearLectorAsync();
        for (var i = 0; i < 3; i++)
        {
            var libroPrestado = await CrearLibroAsync(stock: 1);
            await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
            {
                LectorId = lector.Id,
                LibroId = libroPrestado.Id
            });
        }
        var libro = await CrearLibroAsync(stock: 1);

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WhenLectorInMora_Returns403Forbidden()
    {
        var libroVencido = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
            context.Prestamos.Add(new Prestamo
            {
                LectorId = lector.Id,
                LibroId = libroVencido.Id,
                FechaPrestamo = DateTime.UtcNow.AddDays(-20),
                FechaVencimiento = DateTime.UtcNow.AddDays(-6),
                Estado = EstadoPrestamo.Activo
            });
            await context.SaveChangesAsync();
        }
        var libro = await CrearLibroAsync(stock: 1);

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDevolucion_WhenLate_SuspendsLectorAndNextPrestamoReturns403Forbidden()
    {
        var libroVencido = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamoVencido = new Prestamo
        {
            LectorId = lector.Id,
            LibroId = libroVencido.Id,
            FechaPrestamo = DateTime.UtcNow.AddDays(-17),
            FechaVencimiento = DateTime.UtcNow.AddDays(-3),
            Estado = EstadoPrestamo.Activo
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
            context.Prestamos.Add(prestamoVencido);
            await context.SaveChangesAsync();
        }
        var prestamoVencidoId = prestamoVencido.Id;

        var devolucion = await _client.PutAsync($"/api/prestamos/{prestamoVencidoId}/devolucion", content: null);
        Assert.Equal(HttpStatusCode.OK, devolucion.StatusCode);
        var devuelto = await devolucion.Content.ReadFromJsonAsync<PrestamoResponseDTO>();

        var lectorActual = await _client.GetFromJsonAsync<LectorResponseDTO>($"/api/lectores/{lector.Id}");
        Assert.NotNull(lectorActual!.SuspendidoHasta);
        Assert.Equal(devuelto!.FechaDevolucion!.Value.AddDays(3), lectorActual.SuspendidoHasta!.Value, TimeSpan.FromSeconds(1));

        var libro = await CrearLibroAsync(stock: 1);
        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDevolucion_OnTime_DoesNotSuspendLector()
    {
        var libro = await CrearLibroAsync(stock: 2);
        var lector = await CrearLectorAsync();
        var creadoResponse = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });
        var creado = await creadoResponse.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        await _client.PutAsync($"/api/prestamos/{creado!.Id}/devolucion", content: null);

        var lectorActual = await _client.GetFromJsonAsync<LectorResponseDTO>($"/api/lectores/{lector.Id}");
        Assert.Null(lectorActual!.SuspendidoHasta);

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<PrestamoResponseDTO> CrearPrestamoAsync(int lectorId, int libroId)
    {
        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lectorId,
            LibroId = libroId
        });
        return (await response.Content.ReadFromJsonAsync<PrestamoResponseDTO>())!;
    }

    [Fact]
    public async Task Extravio_BlocksLectorUntilReposicionAndThenRestoresStock()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var otroLibro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamo = await CrearPrestamoAsync(lector.Id, libro.Id);

        var extravio = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/extravio", content: null);
        Assert.Equal(HttpStatusCode.OK, extravio.StatusCode);
        var extraviado = await extravio.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        Assert.Equal("Extraviado", extraviado!.Estado);
        var libroSinReponer = await _client.GetFromJsonAsync<LibroResponseDTO>($"/api/libros/{libro.Id}");
        Assert.Equal(0, libroSinReponer!.Stock);

        var bloqueado = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = otroLibro.Id
        });
        Assert.Equal(HttpStatusCode.Forbidden, bloqueado.StatusCode);

        var reposicion = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/reposicion", content: null);
        Assert.Equal(HttpStatusCode.OK, reposicion.StatusCode);
        var repuesto = await reposicion.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        Assert.Equal("Repuesto", repuesto!.Estado);
        var libroRepuesto = await _client.GetFromJsonAsync<LibroResponseDTO>($"/api/libros/{libro.Id}");
        Assert.Equal(1, libroRepuesto!.Stock);

        var desbloqueado = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = otroLibro.Id
        });
        Assert.Equal(HttpStatusCode.Created, desbloqueado.StatusCode);
    }

    [Fact]
    public async Task Reposicion_WhenPrestamoIsNotExtraviado_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamo = await CrearPrestamoAsync(lector.Id, libro.Id);

        var response = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/reposicion", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Extravio_WhenPrestamoAlreadyReturned_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamo = await CrearPrestamoAsync(lector.Id, libro.Id);
        await _client.PutAsync($"/api/prestamos/{prestamo.Id}/devolucion", content: null);

        var response = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/extravio", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Extravio_WithUnknownId_Returns404NotFound()
    {
        var response = await _client.PutAsync($"/api/prestamos/{int.MaxValue}/extravio", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLector_WithExtraviadoPrestamo_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamo = await CrearPrestamoAsync(lector.Id, libro.Id);
        await _client.PutAsync($"/api/prestamos/{prestamo.Id}/extravio", content: null);

        var response = await _client.DeleteAsync($"/api/lectores/{lector.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLectorAndLibro_WithReturnedLoanHistory_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamo = await CrearPrestamoAsync(lector.Id, libro.Id);
        await _client.PutAsync($"/api/prestamos/{prestamo.Id}/devolucion", content: null);

        var borrarLector = await _client.DeleteAsync($"/api/lectores/{lector.Id}");
        var borrarLibro = await _client.DeleteAsync($"/api/libros/{libro.Id}");

        Assert.Equal(HttpStatusCode.Conflict, borrarLector.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, borrarLibro.StatusCode);
    }
}
