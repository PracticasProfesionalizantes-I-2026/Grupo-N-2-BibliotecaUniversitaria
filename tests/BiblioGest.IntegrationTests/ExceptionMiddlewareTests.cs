using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.DTOs.Libros;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class ExceptionMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ExceptionMiddlewareTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnhandledException_Returns500InternalServerError()
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<ILibroService, LibroServiceQueFalla>()))
            .CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "admin@bibliogest.com",
            Password = "Admin123!"
        });
        var token = (await login.Content.ReadFromJsonAsync<LoginResponseDTO>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private class LibroServiceQueFalla : ILibroService
    {
        public Task<IReadOnlyList<LibroResponseDTO>> GetAllAsync(string? busqueda, CancellationToken ct = default) =>
            throw new InvalidOperationException("Falla simulada");

        public Task<LibroResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            throw new InvalidOperationException("Falla simulada");

        public Task<LibroResponseDTO> CreateAsync(LibroCreateDTO dto, CancellationToken ct = default) =>
            throw new InvalidOperationException("Falla simulada");

        public Task<LibroResponseDTO> UpdateAsync(Guid id, LibroUpdateDTO dto, CancellationToken ct = default) =>
            throw new InvalidOperationException("Falla simulada");

        public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
            throw new InvalidOperationException("Falla simulada");
    }
}
