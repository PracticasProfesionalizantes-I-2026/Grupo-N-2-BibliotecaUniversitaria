using System.Net;
using System.Net.Http.Json;
using BiblioGest.Shared.DTOs.Reportes;
using Xunit;

namespace BiblioGest.IntegrationTests;

public class ReportesControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public ReportesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTipos_ReturnsReportesDisponibles()
    {
        var tipos = await _client.GetFromJsonAsync<List<ReporteTipoDTO>>("/api/reportes");

        Assert.NotNull(tipos);
        Assert.Equal(3, tipos!.Count);
    }

    [Fact]
    public async Task GetReporte_ReturnsSuccessAndData()
    {
        var response = await _client.GetAsync("/api/reportes/resumen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reporte = await response.Content.ReadFromJsonAsync<ReporteResponseDTO>();
        Assert.NotNull(reporte!.Resumen);
        Assert.True(reporte.Resumen!.TotalLibros > 0);
    }

    [Fact]
    public async Task GetReporte_Mora_ReturnsPrestamosVencidos()
    {
        var reporte = await _client.GetFromJsonAsync<ReporteResponseDTO>("/api/reportes/mora");

        Assert.NotEmpty(reporte!.Prestamos);
        Assert.All(reporte.Prestamos, p => Assert.True(p.EnMora));
    }

    [Fact]
    public async Task GetReporte_WithNoData_Returns200OKEmpty()
    {
        var response = await _client.GetAsync("/api/reportes/prestamos?desde=2000-01-01&hasta=2000-01-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reporte = await response.Content.ReadFromJsonAsync<ReporteResponseDTO>();
        Assert.Empty(reporte!.Prestamos);
        Assert.NotNull(reporte.Mensaje);
    }

    [Fact]
    public async Task GetReporte_WithInvalidFilter_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/reportes/prestamos?desde=2026-10-10&hasta=2026-10-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReporte_WithUnknownType_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/reportes/ventas");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReporte_WithoutToken_Returns401Unauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/reportes/resumen");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
