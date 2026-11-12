using System.Net.Http.Json;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.DTOs.Reservas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class DeleteBehaviorReservasTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public DeleteBehaviorReservasTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteLibro_WithActiveReserva_ThrowsDbUpdateExceptionAtDatabaseLevel()
    {
        var libro = (await (await _client.PostAsJsonAsync("/api/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = $"978{Random.Shared.NextInt64(1_000_000_000, 10_000_000_000)}",
            Ubicacion = "E5-4",
            Stock = 0
        })).Content.ReadFromJsonAsync<LibroResponseDTO>())!;

        var lector = (await (await _client.PostAsJsonAsync("/api/lectores", new LectorCreateDTO
        {
            Nombre = "Reserva",
            Apellido = "Test",
            Email = $"{Guid.NewGuid()}@example.com",
            Dni = Random.Shared.Next(10_000_000, 100_000_000).ToString()
        })).Content.ReadFromJsonAsync<LectorResponseDTO>())!;

        await _client.PostAsJsonAsync("/api/reservas", new ReservaCreateDTO { LectorId = lector.Id, LibroId = libro.Id });

        using var scope = _factory.Services.CreateScope();
        var libroRepository = scope.ServiceProvider.GetRequiredService<ILibroRepository>();
        var libroEntidad = await libroRepository.GetByIdAsync(libro.Id);

        await Assert.ThrowsAsync<DbUpdateException>(() => libroRepository.DeleteAsync(libroEntidad!));
    }

    [Fact]
    public async Task DeleteLector_WithActiveReserva_ThrowsDbUpdateExceptionAtDatabaseLevel()
    {
        var libro = (await (await _client.PostAsJsonAsync("/api/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = $"978{Random.Shared.NextInt64(1_000_000_000, 10_000_000_000)}",
            Ubicacion = "E5-5",
            Stock = 0
        })).Content.ReadFromJsonAsync<LibroResponseDTO>())!;

        var lector = (await (await _client.PostAsJsonAsync("/api/lectores", new LectorCreateDTO
        {
            Nombre = "Reserva",
            Apellido = "Test",
            Email = $"{Guid.NewGuid()}@example.com",
            Dni = Random.Shared.Next(10_000_000, 100_000_000).ToString()
        })).Content.ReadFromJsonAsync<LectorResponseDTO>())!;

        await _client.PostAsJsonAsync("/api/reservas", new ReservaCreateDTO { LectorId = lector.Id, LibroId = libro.Id });

        using var scope = _factory.Services.CreateScope();
        var lectorRepository = scope.ServiceProvider.GetRequiredService<ILectorRepository>();
        var lectorEntidad = await lectorRepository.GetByIdAsync(lector.Id);

        await Assert.ThrowsAsync<DbUpdateException>(() => lectorRepository.DeleteAsync(lectorEntidad!));
    }
}
