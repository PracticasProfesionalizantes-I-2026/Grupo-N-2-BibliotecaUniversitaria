using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.Exceptions;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class LectorServiceTests
{
    private readonly Mock<ILectorRepository> _lectorRepository = new();
    private readonly Mock<IPrestamoRepository> _prestamoRepository = new();
    private readonly Mock<IReservaRepository> _reservaRepository = new();

    private LectorService CrearService() => new(_lectorRepository.Object, _prestamoRepository.Object, _reservaRepository.Object);

    [Fact]
    public async Task CreateLectorAsync_WithValidData_SavesAndReturnsCreatedLector()
    {
        var dto = new LectorCreateDTO
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@example.com",
            Dni = "30111222"
        };

        _lectorRepository
            .Setup(r => r.GetByDniAsync(dto.Dni, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector?)null);
        _lectorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector l, CancellationToken _) =>
            {
                l.Id = IdsDePrueba.Nuevo();
                return l;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.True(resultado.Id > 0);
        Assert.Equal(dto.Dni, resultado.Dni);
    }

    [Fact]
    public async Task CreateLectorAsync_WithMissingRequiredField_ThrowsValidationException()
    {
        var dto = new LectorCreateDTO { Nombre = "", Apellido = "Gómez", Email = "a@a.com", Dni = "1" };

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLectorAsync_WhenDniAlreadyExists_ThrowsConflictException()
    {
        var dto = new LectorCreateDTO
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@example.com",
            Dni = "30111222"
        };

        _lectorRepository
            .Setup(r => r.GetByDniAsync(dto.Dni, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lector { Id = IdsDePrueba.Nuevo(), Dni = dto.Dni });

        var service = CrearService();

        await Assert.ThrowsAsync<DniDuplicadoException>(() => service.CreateAsync(dto));
        _lectorRepository.Verify(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLectorByIdAsync_WhenLectorDoesNotExist_ThrowsNotFoundException()
    {
        var id = IdsDePrueba.Nuevo();
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Lector?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorNotFoundException>(() => service.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteLectorAsync_WithoutActiveLoans_DeletesLector()
    {
        var id = IdsDePrueba.Nuevo();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Dni = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.TieneActivosPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CrearService();
        await service.DeleteAsync(id);

        _lectorRepository.Verify(r => r.DeleteAsync(lector, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLectorAsync_WhenLectorHasActiveLoans_ThrowsConflictException()
    {
        var id = IdsDePrueba.Nuevo();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Dni = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.TieneActivosPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorConPrestamosActivosException>(() => service.DeleteAsync(id));
    }

    [Fact]
    public async Task UpdateLectorAsync_WithValidData_UpdatesLector()
    {
        var id = IdsDePrueba.Nuevo();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Dni = "30111222" };
        var dto = new LectorUpdateDTO { Nombre = "Ana María", Apellido = "Gómez", Email = "anamaria@example.com", Dni = "30111222" };
        _lectorRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);
        _lectorRepository
            .Setup(r => r.GetByDniAsync(dto.Dni, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);

        var service = CrearService();
        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("Ana María", resultado.Nombre);
        Assert.Equal("anamaria@example.com", resultado.Email);
        _lectorRepository.Verify(r => r.UpdateAsync(lector, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLectorAsync_WhenDniBelongsToOtherLector_ThrowsConflictException()
    {
        var id = IdsDePrueba.Nuevo();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Dni = "30111222" };
        var otro = new Lector { Id = IdsDePrueba.Nuevo(), Nombre = "Luis", Apellido = "Paz", Email = "luis@example.com", Dni = "40999888" };
        var dto = new LectorUpdateDTO { Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Dni = "40999888" };
        _lectorRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);
        _lectorRepository
            .Setup(r => r.GetByDniAsync(dto.Dni, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otro);

        var service = CrearService();

        await Assert.ThrowsAsync<DniDuplicadoException>(() => service.UpdateAsync(id, dto));
        _lectorRepository.Verify(r => r.UpdateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static LectorCreateDTO LectorValidoDto() => new()
    {
        Nombre = "Ana",
        Apellido = "Gómez",
        Email = "ana@example.com",
        Dni = "30111222"
    };

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("ana@example")]
    [InlineData("ana gomez@example.com")]
    public async Task CreateLectorAsync_WithInvalidEmail_ThrowsLectorInvalidoException(string email)
    {
        var dto = LectorValidoDto();
        dto.Email = email;

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("123456789")]
    [InlineData("30.111.222")]
    [InlineData("30 111 222")]
    [InlineData("L-4521")]
    [InlineData("ABC12345")]
    public async Task CreateLectorAsync_WithInvalidDni_ThrowsLectorInvalidoException(string dni)
    {
        var dto = LectorValidoDto();
        dto.Dni = dni;

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
        _lectorRepository.Verify(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("1234567", "1234567")]
    [InlineData("30111222", "30111222")]
    [InlineData(" 30111222 ", "30111222")]
    public async Task CreateLectorAsync_WithValidDni_SavesItTrimmed(string dni, string esperado)
    {
        var dto = LectorValidoDto();
        dto.Dni = dni;
        _lectorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector l, CancellationToken _) => l);

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.Equal(esperado, resultado.Dni);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("Ana  María")]
    [InlineData("Ana123")]
    [InlineData("Ana_María")]
    [InlineData("-Ana")]
    public async Task CreateLectorAsync_WithInvalidNombre_ThrowsLectorInvalidoException(string nombre)
    {
        var dto = LectorValidoDto();
        dto.Nombre = nombre;

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
        _lectorRepository.Verify(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLectorAsync_WithNombreTooLong_ThrowsLectorInvalidoException()
    {
        var dto = LectorValidoDto();
        dto.Nombre = new string('a', 51);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLectorAsync_WithApellidoTooShort_ThrowsLectorInvalidoException()
    {
        var dto = LectorValidoDto();
        dto.Apellido = "G";

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Theory]
    [InlineData("Al")]
    [InlineData("María José")]
    [InlineData("O'Connor")]
    [InlineData("Pérez-Núñez")]
    public async Task CreateLectorAsync_WithValidNombre_CreatesLector(string nombre)
    {
        var dto = LectorValidoDto();
        dto.Nombre = nombre;
        _lectorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector l, CancellationToken _) => l);

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.Equal(nombre, resultado.Nombre);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("a@b.c")]
    public async Task CreateLectorAsync_WithEmailOutOfRange_ThrowsLectorInvalidoException(string email)
    {
        var dto = LectorValidoDto();
        dto.Email = email;

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLectorAsync_WithEmailTooLong_ThrowsLectorInvalidoException()
    {
        var dto = LectorValidoDto();
        dto.Email = new string('a', 89) + "@example.com";

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task DeleteLectorAsync_WhenLectorHasActiveReservas_ThrowsConflictException()
    {
        var id = IdsDePrueba.Nuevo();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Dni = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _reservaRepository.Setup(r => r.TieneActivasPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorConReservasActivasException>(() => service.DeleteAsync(id));
        _lectorRepository.Verify(r => r.DeleteAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLectorAsync_WhenLectorHasLoanHistory_ThrowsConflictException()
    {
        var id = IdsDePrueba.Nuevo();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Dni = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.TienePrestamosPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorConHistorialException>(() => service.DeleteAsync(id));
        _lectorRepository.Verify(r => r.DeleteAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLectorAsync_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        var dto = LectorValidoDto();
        dto.Email = "  ANA@Example.com ";
        _lectorRepository
            .Setup(r => r.GetByEmailAsync("ana@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lector { Id = IdsDePrueba.Nuevo(), Email = "ana@example.com" });

        var service = CrearService();

        await Assert.ThrowsAsync<LectorEmailDuplicadoException>(() => service.CreateAsync(dto));
        _lectorRepository.Verify(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLectorAsync_KeepingOwnEmail_DoesNotThrow()
    {
        var id = IdsDePrueba.Nuevo();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Dni = "30111222" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _lectorRepository.Setup(r => r.GetByEmailAsync("ana@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        var dto = new LectorUpdateDTO { Nombre = "Ana", Apellido = "Gómez", Email = "Ana@Example.com", Dni = "30111222" };

        var service = CrearService();
        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("ana@example.com", resultado.Email);
    }
}
