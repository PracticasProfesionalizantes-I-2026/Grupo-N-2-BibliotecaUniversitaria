using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BiblioGest.Shared.DTOs.Auth;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BiblioGest.IntegrationTests;

// Cubre los requests protegidos sin token, con token vencido y con token
// firmado con una clave distinta (equivalente a "modificado a mano").
public class TokenValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Key = "UnaClaveSecretaBienLargaYDificilDeAdivinar123!";
    private const string Issuer = "BiblioGestApi";
    private const string Audience = "BiblioGestClient";

    private readonly CustomWebApplicationFactory _factory;

    public TokenValidationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static string CrearToken(DateTime expira, string key = Key)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "admin@bibliogest.com"),
            new Claim(ClaimTypes.Role, "Administrador")
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: expira, signingCredentials: credenciales);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var tokenVencido = CrearToken(DateTime.UtcNow.AddMinutes(-5));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenVencido);

        var response = await client.GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedSignature_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var tokenFirmadoConOtraClave = CrearToken(DateTime.UtcNow.AddMinutes(30), key: "OtraClaveCompletamenteDistintaYLarga!!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenFirmadoConOtraClave);

        var response = await client.GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenEditedByHand_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "admin@bibliogest.com",
            Password = "Admin123!"
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        var tokenModificado = login!.Token[..^1] + (login.Token[^1] == 'A' ? 'B' : 'A');
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenModificado);

        var response = await client.GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithGarbageToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "esto-no-es-un-token-valido");

        var response = await client.GetAsync("/api/libros");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsSpanishMessage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/libros");

        var body = await response.Content.ReadFromJsonAsync<MensajeRespuesta>();
        Assert.Equal("Debe iniciar sesión.", body!.Message);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsSpanishMessage()
    {
        var client = _factory.CreateClient();
        var tokenFirmadoConOtraClave = CrearToken(DateTime.UtcNow.AddMinutes(30), key: "OtraClaveCompletamenteDistintaYLarga!!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenFirmadoConOtraClave);

        var response = await client.GetAsync("/api/libros");

        var body = await response.Content.ReadFromJsonAsync<MensajeRespuesta>();
        Assert.Equal("Token inválido o vencido.", body!.Message);
    }

    [Fact]
    public async Task Usuarios_AsBibliotecario_ReturnsSpanishForbiddenMessage()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("bibliotecario@bibliogest.com", "Bibliotecario123!");

        var response = await client.GetAsync("/api/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MensajeRespuesta>();
        Assert.Equal("No tiene permisos para realizar esta acción.", body!.Message);
    }

    private sealed record MensajeRespuesta(string Message);
}
