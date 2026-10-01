using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-test-no-usar-en-produccion-1234567890",
            ["Jwt:Issuer"] = "BiblioGest.Tests",
            ["Jwt:Audience"] = "BiblioGest.Tests.Clients",
            ["Jwt:ExpirationMinutes"] = "60"
        })
        .Build();

    private AuthService CrearService() => new(_usuarioRepository.Object, _configuration);

    private static Usuario CrearUsuarioConPassword(string password, bool activo = true, RolUsuario rol = RolUsuario.Bibliotecario)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Carla Pérez",
            Email = "carla@example.com",
            Rol = rol,
            Activo = activo
        };
        usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, password);
        return usuario;
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndRole()
    {
        var usuario = CrearUsuarioConPassword("secreta1", rol: RolUsuario.Administrador);
        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CrearService();
        var resultado = await service.LoginAsync(new LoginRequestDTO { Email = usuario.Email, Password = "secreta1" });

        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));
        Assert.Equal("Administrador", resultado.Rol);
        Assert.Equal(usuario.Email, resultado.Email);
        Assert.True(resultado.ExpiraEn > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedException()
    {
        var usuario = CrearUsuarioConPassword("secreta1");
        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CrearService();

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => service.LoginAsync(new LoginRequestDTO { Email = usuario.Email, Password = "incorrecta" }));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUser_ThrowsUnauthorizedException()
    {
        _usuarioRepository
            .Setup(r => r.GetByEmailAsync("no-existe@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => service.LoginAsync(new LoginRequestDTO { Email = "no-existe@example.com", Password = "secreta1" }));
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsUnauthorizedException()
    {
        var usuario = CrearUsuarioConPassword("secreta1", activo: false);
        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CrearService();

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => service.LoginAsync(new LoginRequestDTO { Email = usuario.Email, Password = "secreta1" }));
    }
}
