using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BiblioGest.DataAccess;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.DTOs.Usuarios;
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
    // La API serializa en snake_case (Program.cs); los tests usan estas
    // mismas opciones al armar y leer JSON para no desalinearse.
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    // Credenciales del Administrador sembrado por DbInitializer (solo desarrollo/tests).
    public const string AdminEmail = "admin@bibliogest.local";
    public const string AdminPassword = "Admin123!";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    // Hace login y devuelve un HttpClient nuevo con el Bearer ya seteado.
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = AdminEmail, string password = AdminPassword, CancellationToken ct = default)
    {
        var client = CreateClient();
        var token = await LoginAsync(client, email, password, ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<string> LoginAsync(
        HttpClient client, string email, string password, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = email,
            Password = password
        }, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(JsonOptions, ct);
        return login!.Token;
    }

    // Crea un usuario con rol Bibliotecario (usando al Administrador sembrado) y
    // devuelve un cliente autenticado como ese usuario, útil para tests de 403.
    public async Task<HttpClient> CreateBibliotecarioClientAsync(CancellationToken ct = default)
    {
        var adminClient = await CreateAuthenticatedClientAsync(ct: ct);

        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "secreta1";
        await adminClient.PostAsJsonAsync("/api/v1/usuarios", new UsuarioCreateDTO
        {
            Nombre = "Bibliotecario de prueba",
            Email = email,
            Password = password,
            Rol = "Bibliotecario"
        }, JsonOptions, ct);

        return await CreateAuthenticatedClientAsync(email, password, ct);
    }

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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
