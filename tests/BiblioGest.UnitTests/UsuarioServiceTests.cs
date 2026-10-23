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

        private UsuarioService CrearService() => new(_usuarioRepository.Object, _passwordHasher.Object);

        private static Usuario UsuarioExistente(Guid id, RolUsuario rol = RolUsuario.Bibliotecario, bool activo = true) => new()
        {
            Id = id,
            Email = "existente@example.com",
            PasswordHash = "hash-original",
            RolUsuario = rol,
            Activo = activo
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
                    u.Id = Guid.NewGuid();
                    return u;
                });

            var service = CrearService();
            var resultado = await service.CreateAsync(dto);

            Assert.NotEqual(Guid.Empty, resultado.Id);
            Assert.Equal(dto.Email, resultado.Email);
            Assert.Equal("Administrador", resultado.RolUsuario);
            _passwordHasher.Verify(h => h.Hash(dto.Password), Times.Once);
            _usuarioRepository.Verify(
                r => r.CreateAsync(It.Is<Usuario>(u => u.PasswordHash == "hash-falso"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData("no-es-un-email")]
        [InlineData("falta-arroba.com")]
        [InlineData("   ")]
        public async Task CreateUsuarioAsync_WithInvalidEmail_ThrowsValidationException(string email)
        {
            var dto = new UsuarioCreateDTO { Email = email, Password = "Password123", RolUsuario = "Administrador" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithTooLongEmail_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO
            {
                Email = $"{new string('a', 150)}@example.com",
                Password = "Password123",
                RolUsuario = "Administrador"
            };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
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
                .ReturnsAsync(UsuarioExistente(Guid.NewGuid()));

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioDuplicadoException>(() => service.CreateAsync(dto));
            _usuarioRepository.Verify(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateUsuarioAsync_WhenEmailDuplicadoWithDifferentCaseAndSpaces_ThrowsConflictException()
        {
            var dto = new UsuarioCreateDTO
            {
                Email = "  Existente@Example.com ",
                Password = "Password123",
                RolUsuario = "Bibliotecario"
            };
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync("existente@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(UsuarioExistente(Guid.NewGuid()));

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioDuplicadoException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithMissingEmail_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO { Email = "", Password = "Password123", RolUsuario = "Administrador" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioInvalidoException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateUsuarioAsync_WithWhitespaceOnlyEmail_ThrowsValidationException()
        {
            var dto = new UsuarioCreateDTO { Email = "   ", Password = "Password123", RolUsuario = "Administrador" };

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
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioNotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WithValidData_UpdatesUsuario()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id);
            var dto = new UsuarioUpdateDTO { Email = "modificado@example.com", RolUsuario = "Administrador" };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

            var service = CrearService();
            var resultado = await service.UpdateAsync(id, dto, Guid.NewGuid());

            Assert.Equal("modificado@example.com", resultado.Email);
            Assert.Equal("Administrador", resultado.RolUsuario);
            Assert.Equal("hash-original", usuario.PasswordHash);
            _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            _usuarioRepository.Verify(r => r.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WithNewPassword_HashesPassword()
        {
            var id = Guid.NewGuid();
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
            await service.UpdateAsync(id, dto, Guid.NewGuid());

            Assert.Equal("hash-nuevo", usuario.PasswordHash);
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WithUnknownId_ThrowsNotFoundException()
        {
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);
            var dto = new UsuarioUpdateDTO { Email = "x@example.com" };

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WhenEmailBelongsToOtherUsuario_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            var dto = new UsuarioUpdateDTO { Email = "otro@example.com" };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UsuarioExistente(id));
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UsuarioExistente(Guid.NewGuid()));

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioDuplicadoException>(() => service.UpdateAsync(id, dto, Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WhenAdminDeactivatesHimself_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id, RolUsuario.Administrador);
            var dto = new UsuarioUpdateDTO { Email = usuario.Email, Activo = false };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioAutoBajaException>(() => service.UpdateAsync(id, dto, currentUserId: id));
            _usuarioRepository.Verify(r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WhenDeactivatingLastActiveAdmin_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id, RolUsuario.Administrador);
            var dto = new UsuarioUpdateDTO { Email = usuario.Email, Activo = false };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.CountAdministradoresActivosAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var service = CrearService();

            await Assert.ThrowsAsync<UltimoAdministradorException>(
                () => service.UpdateAsync(id, dto, currentUserId: Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WhenChangingRoleOfLastActiveAdmin_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id, RolUsuario.Administrador);
            var dto = new UsuarioUpdateDTO { Email = usuario.Email, RolUsuario = "Bibliotecario" };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.CountAdministradoresActivosAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var service = CrearService();

            await Assert.ThrowsAsync<UltimoAdministradorException>(
                () => service.UpdateAsync(id, dto, currentUserId: Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateUsuarioAsync_WhenDeactivatingAdminAndAnotherAdminIsActive_Succeeds()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id, RolUsuario.Administrador);
            var dto = new UsuarioUpdateDTO { Email = usuario.Email, Activo = false };
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.CountAdministradoresActivosAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            var service = CrearService();
            var resultado = await service.UpdateAsync(id, dto, currentUserId: Guid.NewGuid());

            Assert.False(resultado.Activo);
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WithConfirmation_DeletesUsuario()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id);
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);

            var service = CrearService();
            await service.DeleteAsync(id, Guid.NewGuid());

            Assert.False(usuario.Activo);
            _usuarioRepository.Verify(r => r.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WithUnknownId_ThrowsNotFoundException()
        {
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioNotFoundException>(() => service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WhenAdminDeletesHimself_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id, RolUsuario.Administrador);
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);

            var service = CrearService();

            await Assert.ThrowsAsync<UsuarioAutoBajaException>(() => service.DeleteAsync(id, currentUserId: id));
            _usuarioRepository.Verify(r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUsuarioAsync_WhenLastActiveAdmin_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            var usuario = UsuarioExistente(id, RolUsuario.Administrador);
            _usuarioRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _usuarioRepository
                .Setup(r => r.CountAdministradoresActivosAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var service = CrearService();

            await Assert.ThrowsAsync<UltimoAdministradorException>(
                () => service.DeleteAsync(id, currentUserId: Guid.NewGuid()));
        }
    }
}
