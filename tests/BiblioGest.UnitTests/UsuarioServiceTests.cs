using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Usuarios;
using BiblioGest.Shared.Exceptions;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests
{
    public class UsuarioServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();

        private const int AdminLogueadoId = 999_999;

        private UsuarioService CrearService() => new(_usuarioRepository.Object, _passwordHasher.Object);

        private static Usuario UsuarioExistente(int id) => new()
        {
            Id = id,
            Email = "existente@example.com",
            PasswordHash = "hash-original",
            RolUsuario = RolUsuario.Bibliotecario,
            Activo = true
        };

        [Fact]
        public async Task CreateUsuarioAsync_WithValidData_SavesAndReturnsCreatedUsuario()
        {
            var dto = new UsuarioCreateDTO
            {
                Email = "test@example.com",
                Password = "Password123",
                RolUsuario = "Administrador"
            };
            _usuarioRepository
                .Setup(repo => repo.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);
            _passwordHasher
                .Setup(hasher => hasher.Hash(dto.Password))
                .Returns("hash-falso");
            _usuarioRepository
                .Setup(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario u, CancellationToken _) =>
                {
                    u.Id = IdsDePrueba.Nuevo();
                    return u;
                });

            var service = CrearService();
            var resultado = await service.CreateAsync(dto);

            Assert.True(resultado.Id > 0);
            Assert.Equal(dto.Email, resultado.Email);
            Assert.Equal("Administrador", resultado.RolUsuario);
            _passwordHasher.Verify(h => h.Hash(dto.Password), Times.Once);
            _usuarioRepository.Verify(
                r => r.CreateAsync(It.Is<Usuario>(u => u.PasswordHash == "hash-falso"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateUsuarioAsync_WhenEmailAlreadyExists_ThrowsConflictException()
        {
            var dto = new UsuarioCreateDTO
            {
                Email = "existente@example.com",
                Password = "Password123",
                RolUsuario = "Bibliotecario"
            };
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UsuarioExistente(IdsDePrueba.Nuevo()));

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioDuplicadoException>(() => service.CreateAsync(dto));
            _usuarioRepository.Verify(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithMissingEmail_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO { Email = "", Password = "Password123", RolUsuario = "Administrador" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithShortPassword_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO { Email = "nuevo@example.com", Password = "12345", RolUsuario = "Administrador" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithInvalidRol_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO { Email = "nuevo@example.com", Password = "Password123", RolUsuario = "Director" };
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task GetUsuarioByIdAsync_WithUnknownId_ThrowsNotFoundException()
        {
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioNotFoundException>(() => service.GetByIdAsync(IdsDePrueba.Nuevo()));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WithValidData_UpdatesUsuario()
        {
            var id = IdsDePrueba.Nuevo();
            var usuario = UsuarioExistente(id);
            var dto = new UsuarioUpdateDTO { Email = "modificado@example.com", RolUsuario = "Administrador" };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

            var service = CrearService();
            var resultado = await service.UpdateAsync(id, dto, AdminLogueadoId);

            Assert.Equal("modificado@example.com", resultado.Email);
            Assert.Equal("Administrador", resultado.RolUsuario);
            Assert.Equal("hash-original", usuario.PasswordHash);
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            _usuarioRepository.Verify(r => r.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WithNewPassword_HashesPassword()
        {
            var id = IdsDePrueba.Nuevo();
            var usuario = UsuarioExistente(id);
            var dto = new UsuarioUpdateDTO { Email = usuario.Email, Password = "NuevaClave1" };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _passwordHasher
                .Setup(h => h.Hash(dto.Password))
                .Returns("hash-nuevo");

            var service = CrearService();
            await service.UpdateAsync(id, dto, AdminLogueadoId);

            Assert.Equal("hash-nuevo", usuario.PasswordHash);
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WithUnknownId_ThrowsNotFoundException()
        {
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);
            var dto = new UsuarioUpdateDTO { Email = "x@example.com" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioNotFoundException>(() => service.UpdateAsync(IdsDePrueba.Nuevo(), dto, AdminLogueadoId));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WhenEmailBelongsToOtherUsuario_ThrowsConflictException()
        {
            var id = IdsDePrueba.Nuevo();
            var dto = new UsuarioUpdateDTO { Email = "otro@example.com" };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UsuarioExistente(id));
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UsuarioExistente(IdsDePrueba.Nuevo()));

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioDuplicadoException>(() => service.UpdateAsync(id, dto, AdminLogueadoId));
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WithConfirmation_DeletesUsuario()
        {
            var id = IdsDePrueba.Nuevo();
            var usuario = UsuarioExistente(id);
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);

            var service = CrearService();
            await service.DeleteAsync(id, AdminLogueadoId);

            Assert.False(usuario.Activo);
            _usuarioRepository.Verify(r => r.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WithUnknownId_ThrowsNotFoundException()
        {
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioNotFoundException>(() => service.DeleteAsync(IdsDePrueba.Nuevo(), AdminLogueadoId));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("a@b.c")]
        [InlineData("no-es-un-email")]
        [InlineData("juan perez@example.com")]
        [InlineData("juan@example")]
        public async Task CreateUsuarioAsync_WithInvalidEmail_ThrowsValidationException(string email)
        {
            var dto = new UsuarioCreateDTO { Email = email, Password = "Password123", RolUsuario = "Administrador" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
            _usuarioRepository.Verify(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithEmailTooLong_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO { Email = new string('a', 89) + "@example.com", Password = "Password123", RolUsuario = "Administrador" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithPasswordTooLong_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO { Email = "nuevo@example.com", Password = new string('a', 65), RolUsuario = "Administrador" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
            _usuarioRepository.Verify(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private Usuario ConfigurarAdministrador(int id, int administradoresActivos)
        {
            var admin = new Usuario
            {
                Id = id,
                Email = $"admin{id}@bibliogest.com",
                PasswordHash = "hash",
                RolUsuario = RolUsuario.Administrador,
                Activo = true
            };
            _usuarioRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
            _usuarioRepository.Setup(r => r.CountAdministradoresActivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(administradoresActivos);
            return admin;
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WhenDeletingOwnAccount_ThrowsConflictException()
        {
            ConfigurarAdministrador(AdminLogueadoId, administradoresActivos: 2);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioAutoBajaException>(() => service.DeleteAsync(AdminLogueadoId, AdminLogueadoId));
            _usuarioRepository.Verify(r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WhenLastActiveAdministrator_ThrowsConflictException()
        {
            var id = IdsDePrueba.Nuevo();
            ConfigurarAdministrador(id, administradoresActivos: 1);

            var service = CrearService();

            await Assert.ThrowsAsync<UltimoAdministradorException>(() => service.DeleteAsync(id, AdminLogueadoId));
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WhenOtherAdministratorsRemain_DeactivatesAdministrator()
        {
            var id = IdsDePrueba.Nuevo();
            var admin = ConfigurarAdministrador(id, administradoresActivos: 2);

            var service = CrearService();
            await service.DeleteAsync(id, AdminLogueadoId);

            Assert.False(admin.Activo);
        }

        [Theory]
        [InlineData(false, "Administrador")]
        [InlineData(true, "Bibliotecario")]
        public async Task UpdateUsuarioAsync_WhenDeactivatingOrDemotingSelf_ThrowsConflictException(bool activo, string rol)
        {
            var admin = ConfigurarAdministrador(AdminLogueadoId, administradoresActivos: 2);
            var dto = new UsuarioUpdateDTO { Email = admin.Email, RolUsuario = rol, Activo = activo };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioAutoModificacionException>(() => service.UpdateAsync(AdminLogueadoId, dto, AdminLogueadoId));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WhenDemotingLastActiveAdministrator_ThrowsConflictException()
        {
            var id = IdsDePrueba.Nuevo();
            var admin = ConfigurarAdministrador(id, administradoresActivos: 1);
            var dto = new UsuarioUpdateDTO { Email = admin.Email, RolUsuario = "Bibliotecario" };

            var service = CrearService();

            await Assert.ThrowsAsync<UltimoAdministradorException>(() => service.UpdateAsync(id, dto, AdminLogueadoId));
            Assert.Equal(RolUsuario.Administrador, admin.RolUsuario);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateUsuarioAsync_WithoutEmail_KeepsCurrentEmail(string? email)
        {
            var id = IdsDePrueba.Nuevo();
            var usuario = UsuarioExistente(id);
            _usuarioRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
            _usuarioRepository.Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

            var service = CrearService();
            var resultado = await service.UpdateAsync(id, new UsuarioUpdateDTO { Email = email, RolUsuario = "Administrador" }, AdminLogueadoId);

            Assert.Equal("existente@example.com", resultado.Email);
            Assert.Equal("Administrador", resultado.RolUsuario);
        }
    }
}
