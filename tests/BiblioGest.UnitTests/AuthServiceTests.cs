using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.Exceptions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private AuthService CrearService() =>
        new(_usuarioRepository.Object, _passwordHasher.Object, CrearConfiguracion());

    private static IConfiguration CrearConfiguracion() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "UnaClaveSecretaBienLargaYDificilDeAdivinar123!",
                ["Jwt:Issuer"] = "BiblioGestApi",
                ["Jwt:Audience"] = "BiblioGestClient",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_DevuelveTokenYDatosDeUsuario()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = "ana@example.com",
            PasswordHash = "hash-cualquiera",
            RolUsuario = RolUsuario.Bibliotecario,
            Activo = true
        };

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher
            .Setup(h => h.Verify("Password123!", usuario.PasswordHash))
            .Returns(true);

        var service = CrearService();
        var dto = new LoginRequestDTO { Email = usuario.Email, Password = "Password123!" };

        var resultado = await service.LoginAsync(dto);

        Assert.False(string.IsNullOrEmpty(resultado.Token));
        Assert.Equal(usuario.Email, resultado.Email);
        Assert.Equal(usuario.RolUsuario.ToString(), resultado.Rol);
    }

    [Fact]
    public async Task LoginAsync_ConEmailInexistente_LanzaCredencialesInvalidasException()
    {
        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CrearService();
        var dto = new LoginRequestDTO { Email = "inexistente@example.com", Password = "Password123!" };

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_ConPasswordIncorrecta_LanzaCredencialesInvalidasException()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = "ana@example.com",
            PasswordHash = "hash-cualquiera",
            RolUsuario = RolUsuario.Bibliotecario,
            Activo = true
        };

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher
            .Setup(h => h.Verify("PasswordIncorrecta!", usuario.PasswordHash))
            .Returns(false);

        var service = CrearService();
        var dto = new LoginRequestDTO { Email = usuario.Email, Password = "PasswordIncorrecta!" };

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioInactivo_LanzaCredencialesInvalidasException()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = "ana@example.com",
            PasswordHash = "hash-cualquiera",
            RolUsuario = RolUsuario.Bibliotecario,
            Activo = false
        };

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher
            .Setup(h => h.Verify("Password123!", usuario.PasswordHash))
            .Returns(true);

        var service = CrearService();
        var dto = new LoginRequestDTO { Email = usuario.Email, Password = "Password123!" };

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => service.LoginAsync(dto));
    }
}