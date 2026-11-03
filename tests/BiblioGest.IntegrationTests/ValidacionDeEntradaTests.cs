using System.Net;
using System.Net.Http.Json;
using System.Text;
using BiblioGest.Shared.DTOs.Auth;
using Xunit;

namespace BiblioGest.IntegrationTests;

// Cubre los casos en los que, antes de llegar a un service, [ApiController]
// rechaza la petición por si solo (campo null, body vacío, JSON mal
// formado): deben responder 400 con el mismo formato { message } en
// español que usa el resto de la API, no el ProblemDetails en inglés por
// defecto de ASP.NET.
public class ValidacionDeEntradaTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ValidacionDeEntradaTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => _client = await _factory.CreateAuthenticatedClientAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record MensajeRespuesta(string Message);

    [Fact]
    public async Task CreateLibro_WithNullTitulo_Returns400WithSpanishMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/libros", new
        {
            titulo = (string?)null,
            autor = "Autor",
            isbn = "9780307474728",
            ubicacion = "A1",
            stock = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MensajeRespuesta>();
        Assert.Equal("El campo Titulo es obligatorio.", body!.Message);
    }

    [Fact]
    public async Task CreateLibro_WithEmptyBody_Returns400WithSpanishMessage()
    {
        var response = await _client.PostAsync("/api/libros",
            new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MensajeRespuesta>();
        Assert.Equal("El cuerpo de la solicitud es obligatorio y no puede estar vacío.", body!.Message);
    }

    [Fact]
    public async Task CreateLibro_WithMalformedJson_Returns400WithSpanishMessage()
    {
        var response = await _client.PostAsync("/api/libros",
            new StringContent("{titulo: \"sin comillas\",,,}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MensajeRespuesta>();
        Assert.Equal("El cuerpo de la solicitud no es un JSON válido.", body!.Message);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400WithSpanishMessage()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "no-es-un-email",
            Password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MensajeRespuesta>();
        Assert.Equal("El campo Email no tiene un formato de email válido.", body!.Message);
    }
}
