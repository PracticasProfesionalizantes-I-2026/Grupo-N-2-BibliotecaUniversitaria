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
            Isbn = IsbnTestHelper.NuevoIsbnValido(),
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

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CreatePrestamo_WithEmptyGuid_Returns400BadRequest(bool lectorVacio, bool libroVacio)
    {
        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lectorVacio ? Guid.Empty : Guid.NewGuid(),
            LibroId = libroVacio ? Guid.Empty : Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WhenLectorAlreadyHasActiveLoanOfSameLibro_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 2);
        var lector = await CrearLectorAsync();
        await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id });

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_ReturnsDiasRestantesYMensajeCalculados()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        var creado = await response.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        Assert.Equal(14, creado!.DiasRestantes);
        Assert.Equal(0, creado.DiasDeAtraso);
        Assert.Equal("Vence en 14 días.", creado.Mensaje);
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
        Assert.Null(actualizado.DiasRestantes);
        Assert.Equal(0, actualizado.DiasDeAtraso);
        Assert.Equal("Devuelto a tiempo.", actualizado.Mensaje);
    }

    [Fact]
    public async Task RegisterDevolucion_WhenOverdue_ReturnsDevueltoConAtraso()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var creadoResponse = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });
        var creado = await creadoResponse.Content.ReadFromJsonAsync<PrestamoResponseDTO>();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
            var prestamo = await context.Prestamos.FindAsync(creado!.Id);
            prestamo!.FechaVencimiento = DateTime.UtcNow.AddDays(-5);
            await context.SaveChangesAsync();
        }

        var response = await _client.PutAsync($"/api/prestamos/{creado!.Id}/devolucion", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        Assert.Equal("Devuelto con atraso", actualizado!.Estado);
        Assert.Null(actualizado.DiasRestantes);
        Assert.Equal(5, actualizado.DiasDeAtraso);
        Assert.Equal("Devuelto con 5 días de atraso.", actualizado.Mensaje);
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

    [Fact]
    public async Task CreatePrestamo_WhenLectorHasThreeActiveLoans_Returns409Conflict()
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

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrestamo_WhenLectorInMora_Returns409Conflict()
    {
        var libroVencido = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();

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
        var libro = await CrearLibroAsync(stock: 1);

        var response = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = lector.Id,
            LibroId = libro.Id
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetPrestamosActivos_ReturnsOnlyActiveOrderedByFechaVencimiento()
    {
        var lector = await CrearLectorAsync();
        var libroVencido = await CrearLibroAsync(stock: 1);
        var libroActivo = await CrearLibroAsync(stock: 1);
        var libroDevuelto = await CrearLibroAsync(stock: 1);

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
        // Otro lector: el primero ya está en mora y no podría pedir este préstamo.
        var otroLector = await CrearLectorAsync();
        var activoResponse = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = otroLector.Id, LibroId = libroActivo.Id });
        Assert.Equal(HttpStatusCode.Created, activoResponse.StatusCode);
        var devueltoResponse = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO
        {
            LectorId = otroLector.Id,
            LibroId = libroDevuelto.Id
        });
        var devuelto = await devueltoResponse.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        await _client.PutAsync($"/api/prestamos/{devuelto!.Id}/devolucion", null);

        var response = await _client.GetAsync("/api/prestamos/activos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var activos = (await response.Content.ReadFromJsonAsync<List<PrestamoResponseDTO>>())!;
        Assert.DoesNotContain(activos, p => p.Id == devuelto.Id);
        Assert.All(activos, p => Assert.NotEqual("Devuelto", p.Estado));
        Assert.Equal(activos.OrderBy(p => p.FechaVencimiento).Select(p => p.Id), activos.Select(p => p.Id));
        Assert.Contains(activos, p => p.LibroId == libroVencido.Id && p.EnMora && p.Estado == "Vencido");
        Assert.Contains(activos, p => p.LibroId == libroActivo.Id && !p.EnMora && p.Estado == "Activo");
    }

    [Fact]
    public async Task GetEstadoLector_WhenLectorCanBorrow_ReturnsPuedePedirAndFechasPrevistas()
    {
        var lector = await CrearLectorAsync();

        var response = await _client.GetAsync($"/api/prestamos/estado-lector/{lector.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var estado = (await response.Content.ReadFromJsonAsync<EstadoLectorPrestamoDTO>())!;
        Assert.True(estado.PuedePedir);
        Assert.Null(estado.Motivo);
        Assert.False(estado.EnMora);
        Assert.Equal(0, estado.PrestamosActivos);
        Assert.Equal(3, estado.MaximoPrestamos);
        Assert.Equal(estado.FechaPrestamo.AddDays(14), estado.FechaVencimiento);
    }

    [Fact]
    public async Task GetEstadoLector_WhenLectorHasThreeActiveLoans_ReturnsSameMotivoAsCreate()
    {
        var lector = await CrearLectorAsync();
        for (var i = 0; i < 3; i++)
        {
            var libroPrestado = await CrearLibroAsync(stock: 1);
            await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libroPrestado.Id });
        }
        var libro = await CrearLibroAsync(stock: 1);
        var crear = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id });
        var error = await crear.Content.ReadFromJsonAsync<MensajeRespuesta>();

        var estado = await _client.GetFromJsonAsync<EstadoLectorPrestamoDTO>($"/api/prestamos/estado-lector/{lector.Id}");

        Assert.False(estado!.PuedePedir);
        Assert.Equal(3, estado.PrestamosActivos);
        Assert.Equal(error!.Message, estado.Motivo);
    }

    [Fact]
    public async Task GetEstadoLector_WhenLectorInMora_ReturnsSameMotivoAsCreate()
    {
        var lector = await CrearLectorAsync();
        var libroVencido = await CrearLibroAsync(stock: 1);
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
        var libro = await CrearLibroAsync(stock: 1);
        var crear = await _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lector.Id, LibroId = libro.Id });
        var error = await crear.Content.ReadFromJsonAsync<MensajeRespuesta>();

        var estado = await _client.GetFromJsonAsync<EstadoLectorPrestamoDTO>($"/api/prestamos/estado-lector/{lector.Id}");

        Assert.False(estado!.PuedePedir);
        Assert.True(estado.EnMora);
        Assert.Equal(error!.Message, estado.Motivo);
    }

    [Fact]
    public async Task GetEstadoLector_WithUnknownLector_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/prestamos/estado-lector/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record MensajeRespuesta(string Message);
}
