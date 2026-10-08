using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Auth;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsOkAndToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "admin@bibliogest.com",
            Password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Administrador", body.Rol);
    }

    [Fact]
    public async Task Login_WithValidBibliotecarioCredentials_ReturnsOkAndToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "bibliotecario@bibliogest.com",
            Password = "Bibliotecario123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Bibliotecario", body.Rol);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "admin@bibliogest.com",
            Password = "ContraseñaIncorrecta"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "noexiste@bibliogest.com",
            Password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_Returns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "",
            Password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "no-es-un-email",
            Password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_Returns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "admin@bibliogest.com",
            Password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
