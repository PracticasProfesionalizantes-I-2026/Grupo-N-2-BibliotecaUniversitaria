using System.Net.Http.Json;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Libros;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class ConcurrenciaStockTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ConcurrenciaStockTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdateAsync_WhenRowVersionIsStale_ThrowsDbUpdateConcurrencyException()
    {
        var response = await _client.PostAsJsonAsync("/api/libros", new LibroCreateDTO
        {
            Titulo = $"Libro {Guid.NewGuid()}",
            Autor = "Autor",
            Isbn = $"978{Random.Shared.NextInt64(1_000_000_000, 10_000_000_000)}",
            Ubicacion = "D4-3",
            Stock = 1
        });
        var libro = (await response.Content.ReadFromJsonAsync<LibroResponseDTO>())!;

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var repoA = scopeA.ServiceProvider.GetRequiredService<ILibroRepository>();
        var repoB = scopeB.ServiceProvider.GetRequiredService<ILibroRepository>();

        var libroA = await repoA.GetByIdAsync(libro.Id);
        var libroB = await repoB.GetByIdAsync(libro.Id);

        libroA!.Stock -= 1;
        await repoA.UpdateAsync(libroA);

        libroB!.Stock -= 1;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.UpdateAsync(libroB));
    }
}
