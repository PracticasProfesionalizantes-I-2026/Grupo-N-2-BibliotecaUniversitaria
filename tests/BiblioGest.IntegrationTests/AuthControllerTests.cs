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
    public async Task Login_ReturnsSuccessAndToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = CustomWebApplicationFactory.AdminEmail,
            Password = CustomWebApplicationFactory.AdminPassword
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDTO>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login!.Token));
        Assert.Equal("Administrador", login.Rol);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = CustomWebApplicationFactory.AdminEmail,
            Password = "contraseña-incorrecta"
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_Returns401Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = "no-existe@example.com",
            Password = "cualquiera1"
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPasswordAndUnknownUser_ReturnSameErrorMessage()
    {
        var wrongPasswordResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = CustomWebApplicationFactory.AdminEmail,
            Password = "contraseña-incorrecta"
        }, CustomWebApplicationFactory.JsonOptions);

        var unknownUserResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = "no-existe@example.com",
            Password = "cualquiera1"
        }, CustomWebApplicationFactory.JsonOptions);

        var wrongPasswordProblem = await wrongPasswordResponse.Content.ReadFromJsonAsync<ProblemDetailsDTO>(CustomWebApplicationFactory.JsonOptions);
        var unknownUserProblem = await unknownUserResponse.Content.ReadFromJsonAsync<ProblemDetailsDTO>(CustomWebApplicationFactory.JsonOptions);

        // CU-01 (3a/3b): mismo código y mismo mensaje, para no revelar cuál
        // dato es el inválido. No se compara el body completo porque
        // ProblemDetails incluye un traceId distinto en cada request.
        Assert.Equal(wrongPasswordResponse.StatusCode, unknownUserResponse.StatusCode);
        Assert.Equal(wrongPasswordProblem!.Title, unknownUserProblem!.Title);
        Assert.Equal(wrongPasswordProblem.Detail, unknownUserProblem.Detail);
    }

    private class ProblemDetailsDTO
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = "no-es-un-email",
            Password = "cualquiera1"
        }, CustomWebApplicationFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
