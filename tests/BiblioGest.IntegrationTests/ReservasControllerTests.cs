using System.Net;
using System.Net.Http.Json;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.DTOs.Reservas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class ReservasControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ReservasControllerTests(CustomWebApplicationFactory factory)
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
            Ubicacion = "B2-3",
            Stock = stock
        });
        return (await response.Content.ReadFromJsonAsync<LibroResponseDTO>())!;
    }

    private async Task<LectorResponseDTO> CrearLectorAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/lectores", new LectorCreateDTO
        {
            Nombre = "Julia",
            Apellido = "Paz",
            Email = $"{Guid.NewGuid()}@example.com",
            Dni = Random.Shared.Next(10_000_000, 100_000_000).ToString()
        });
        return (await response.Content.ReadFromJsonAsync<LectorResponseDTO>())!;
    }

    private Task<HttpResponseMessage> PedirPrestamoAsync(int lectorId, int libroId) =>
        _client.PostAsJsonAsync("/api/prestamos", new PrestamoCreateDTO { LectorId = lectorId, LibroId = libroId });

    private Task<HttpResponseMessage> ReservarAsync(int lectorId, int libroId) =>
        _client.PostAsJsonAsync("/api/reservas", new ReservaCreateDTO { LectorId = lectorId, LibroId = libroId });

    [Fact]
    public async Task Reserva_WhenCopyIsReturned_IsAssignedFor24HoursAndOnlyThatReaderCanBorrowIt()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var poseedor = await CrearLectorAsync();
        var primero = await CrearLectorAsync();
        var segundo = await CrearLectorAsync();
        var prestamo = (await (await PedirPrestamoAsync(poseedor.Id, libro.Id)).Content.ReadFromJsonAsync<PrestamoResponseDTO>())!;

        var respuestaPrimero = await ReservarAsync(primero.Id, libro.Id);
        Assert.Equal(HttpStatusCode.Created, respuestaPrimero.StatusCode);
        var reservaPrimero = await respuestaPrimero.Content.ReadFromJsonAsync<ReservaResponseDTO>();
        Assert.Equal("EnCola", reservaPrimero!.Estado);
        Assert.Equal(1, reservaPrimero.PosicionEnCola);
        var reservaSegundo = await (await ReservarAsync(segundo.Id, libro.Id)).Content.ReadFromJsonAsync<ReservaResponseDTO>();
        Assert.Equal(2, reservaSegundo!.PosicionEnCola);

        var devolucion = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/devolucion", content: null);
        var devuelto = await devolucion.Content.ReadFromJsonAsync<PrestamoResponseDTO>();

        var cola = await _client.GetFromJsonAsync<List<ReservaResponseDTO>>($"/api/reservas?libroId={libro.Id}");
        var asignada = Assert.Single(cola!, r => r.Id == reservaPrimero.Id);
        Assert.Equal("Asignada", asignada.Estado);
        Assert.Equal(devuelto!.FechaDevolucion!.Value.AddHours(24), asignada.FechaLimiteRetiro!.Value, TimeSpan.FromSeconds(1));
        Assert.Equal(1, Assert.Single(cola!, r => r.Id == reservaSegundo.Id).PosicionEnCola);
        var libroActual = await _client.GetFromJsonAsync<LibroResponseDTO>($"/api/libros/{libro.Id}");
        Assert.Equal(0, libroActual!.Stock);

        var otroLector = await PedirPrestamoAsync(segundo.Id, libro.Id);
        Assert.Equal(HttpStatusCode.Conflict, otroLector.StatusCode);

        var lectorAsignado = await PedirPrestamoAsync(primero.Id, libro.Id);
        Assert.Equal(HttpStatusCode.Created, lectorAsignado.StatusCode);
        var reservaConcretada = await _client.GetFromJsonAsync<ReservaResponseDTO>($"/api/reservas/{reservaPrimero.Id}");
        Assert.Equal("Concretada", reservaConcretada!.Estado);
    }

    [Fact]
    public async Task Reserva_WhenAssignmentExpiredAndQueueIsEmpty_ReturnsCopyToStock()
    {
        var libro = await CrearLibroAsync(stock: 0);
        var lector = await CrearLectorAsync();
        var reserva = new Reserva
        {
            LectorId = lector.Id,
            LibroId = libro.Id,
            FechaReserva = DateTime.UtcNow.AddDays(-3),
            FechaAsignacion = DateTime.UtcNow.AddHours(-30),
            FechaLimiteRetiro = DateTime.UtcNow.AddHours(-6),
            Estado = EstadoReserva.Asignada
        };
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
            context.Reservas.Add(reserva);
            await context.SaveChangesAsync();
        }

        var cola = await _client.GetFromJsonAsync<List<ReservaResponseDTO>>($"/api/reservas?libroId={libro.Id}");

        Assert.Empty(cola!);
        var vencida = await _client.GetFromJsonAsync<ReservaResponseDTO>($"/api/reservas/{reserva.Id}");
        Assert.Equal("Vencida", vencida!.Estado);
        var libroActual = await _client.GetFromJsonAsync<LibroResponseDTO>($"/api/libros/{libro.Id}");
        Assert.Equal(1, libroActual!.Stock);
    }

    [Fact]
    public async Task CreateReserva_WhenLibroHasStock_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 2);
        var lector = await CrearLectorAsync();

        var response = await ReservarAsync(lector.Id, libro.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateReserva_WhenDuplicated_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 0);
        var lector = await CrearLectorAsync();
        await ReservarAsync(lector.Id, libro.Id);

        var response = await ReservarAsync(lector.Id, libro.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateReserva_WithUnknownLibro_Returns404NotFound()
    {
        var lector = await CrearLectorAsync();

        var response = await ReservarAsync(lector.Id, int.MaxValue);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelarReserva_ThenCancelAgain_Returns200And409()
    {
        var libro = await CrearLibroAsync(stock: 0);
        var lector = await CrearLectorAsync();
        var reserva = await (await ReservarAsync(lector.Id, libro.Id)).Content.ReadFromJsonAsync<ReservaResponseDTO>();

        var primera = await _client.PutAsync($"/api/reservas/{reserva!.Id}/cancelacion", content: null);
        var segunda = await _client.PutAsync($"/api/reservas/{reserva.Id}/cancelacion", content: null);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal("Cancelada", (await primera.Content.ReadFromJsonAsync<ReservaResponseDTO>())!.Estado);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task GetReserva_WithUnknownId_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/reservas/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLector_WithActiveReserva_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 0);
        var lector = await CrearLectorAsync();
        await ReservarAsync(lector.Id, libro.Id);

        var response = await _client.DeleteAsync($"/api/lectores/{lector.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Renovar_UpToTwoTimes_ThenReturns403Forbidden()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamo = (await (await PedirPrestamoAsync(lector.Id, libro.Id)).Content.ReadFromJsonAsync<PrestamoResponseDTO>())!;

        var primera = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/renovacion", content: null);
        var segunda = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/renovacion", content: null);
        var tercera = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/renovacion", content: null);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        var renovado = await segunda.Content.ReadFromJsonAsync<PrestamoResponseDTO>();
        Assert.Equal(2, renovado!.Renovaciones);
        Assert.Equal(prestamo.FechaVencimiento.AddDays(28), renovado.FechaVencimiento, TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.Forbidden, tercera.StatusCode);
    }

    [Fact]
    public async Task Renovar_WhenOtherReadersAreInQueue_Returns403Forbidden()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var enCola = await CrearLectorAsync();
        var prestamo = (await (await PedirPrestamoAsync(lector.Id, libro.Id)).Content.ReadFromJsonAsync<PrestamoResponseDTO>())!;
        await ReservarAsync(enCola.Id, libro.Id);

        var response = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/renovacion", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Renovar_WhenPrestamoWasReturned_Returns409Conflict()
    {
        var libro = await CrearLibroAsync(stock: 1);
        var lector = await CrearLectorAsync();
        var prestamo = (await (await PedirPrestamoAsync(lector.Id, libro.Id)).Content.ReadFromJsonAsync<PrestamoResponseDTO>())!;
        await _client.PutAsync($"/api/prestamos/{prestamo.Id}/devolucion", content: null);

        var response = await _client.PutAsync($"/api/prestamos/{prestamo.Id}/renovacion", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
