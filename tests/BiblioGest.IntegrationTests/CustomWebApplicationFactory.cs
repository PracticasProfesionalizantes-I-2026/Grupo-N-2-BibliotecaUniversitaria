using System.Net.Http.Headers;
using System.Net.Http.Json;
using BiblioGest.DataAccess;
using BiblioGest.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BiblioGest.IntegrationTests;

// Cada instancia levanta la API completa contra una base SQLite propia en
// memoria (no toca bibliogest.db), para que las pruebas de integración sean
// aisladas entre sí.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BiblioGestDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<BiblioGestDbContext>(options => options.UseSqlite(_connection));
        });
    }

    // Los endpoints de Libros/Lectores/Prestamos llevan [Authorize]: las pruebas
    // de integración necesitan loguearse primero para obtener un token válido.
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "admin@bibliogest.com",
        string password = "Admin123!")
    {
        var client = CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = email,
            Password = password
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
