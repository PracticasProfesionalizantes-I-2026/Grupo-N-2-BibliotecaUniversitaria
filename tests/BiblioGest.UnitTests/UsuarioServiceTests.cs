using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Usuarios;
using BiblioGest.Shared.Exceptions;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();

    private UsuarioService CrearService() => new(_usuarioRepository.Object);

    [Fact]
    public async Task CreateUsuarioAsync_WithValidData_SavesAndReturnsCreatedUsuario()
    {
        var dto = new UsuarioCreateDTO
        {
            Nombre = "Carla Pérez",
            Email = "carla@example.com",
            Password = "secreta1",
            Rol = "Bibliotecario"
        };

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        _usuarioRepository
            .Setup(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario u, CancellationToken _) =>
            {
                u.Id = Guid.NewGuid();
                return u;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(dto.Email, resultado.Email);
        Assert.Equal("Bibliotecario", resultado.Rol);
        Assert.True(resultado.Activo);
        _usuarioRepository.Verify(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUsuarioAsync_WithMissingRequiredField_ThrowsValidationException()
    {
        var dto = new UsuarioCreateDTO { Nombre = "", Email = "a@a.com", Password = "secreta1", Rol = "Bibliotecario" };

        var service = CrearService();

        await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateUsuarioAsync_WithShortPassword_ThrowsValidationException()
    {
        var dto = new UsuarioCreateDTO { Nombre = "Carla", Email = "carla@example.com", Password = "123", Rol = "Bibliotecario" };
        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateUsuarioAsync_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        var dto = new UsuarioCreateDTO
        {
            Nombre = "Carla",
            Email = "carla@example.com",
            Password = "secreta1",
            Rol = "Bibliotecario"
        };

        _usuarioRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = Guid.NewGuid(), Email = dto.Email });

        var service = CrearService();

        await Assert.ThrowsAsync<EmailDuplicadoException>(() => service.CreateAsync(dto));
        _usuarioRepository.Verify(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUsuarioByIdAsync_WhenUsuarioDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _usuarioRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<UsuarioNotFoundException>(() => service.GetByIdAsync(id));
    }

    [Fact]
    public async Task UpdateUsuarioAsync_WithValidData_UpdatesUsuario()
    {
        var id = Guid.NewGuid();
        var usuarioExistente = new Usuario
        {
            Id = id,
            Nombre = "Viejo",
            Email = "viejo@a.com",
            PasswordHash = "hash",
            Rol = RolUsuario.Bibliotecario,
            Activo = true
        };
        _usuarioRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(usuarioExistente);
        _usuarioRepository.Setup(r => r.GetByEmailAsync("nuevo@a.com", It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var dto = new UsuarioUpdateDTO { Nombre = "Nuevo", Email = "nuevo@a.com", Rol = "Administrador" };
        var service = CrearService();

        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("Nuevo", resultado.Nombre);
        Assert.Equal("Administrador", resultado.Rol);
        _usuarioRepository.Verify(r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUsuarioAsync_WithConfirmation_DeletesUsuario()
    {
        var id = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = id,
            Nombre = "N",
            Email = "a@a.com",
            PasswordHash = "hash",
            Rol = RolUsuario.Bibliotecario,
            Activo = true
        };
        _usuarioRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var service = CrearService();
        await service.DeleteAsync(id);

        Assert.False(usuario.Activo);
        _usuarioRepository.Verify(r => r.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }
}
