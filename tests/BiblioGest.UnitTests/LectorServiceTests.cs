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

    private LectorService CrearService() => new(_lectorRepository.Object, _prestamoRepository.Object);

    private static LectorCreateDTO NuevoLectorDto() => new()
    {
        Nombre = "Ana",
        Apellido = "Gómez",
        Email = "ana@example.com",
        Identificador = "30111222"
    };

    private void SetupSinDuplicados()
    {
        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector?)null);
        _lectorRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector?)null);
    }

    [Fact]
    public async Task CreateLectorAsync_WithValidData_SavesAndReturnsCreatedLector()
    {
        var dto = NuevoLectorDto();
        SetupSinDuplicados();
        _lectorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector l, CancellationToken _) =>
            {
                l.Id = Guid.NewGuid();
                return l;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(dto.Identificador, resultado.Identificador);
    }

    [Theory]
    [InlineData("", "Gómez", "a@a.com", "1")]
    [InlineData("Ana", "", "a@a.com", "1")]
    [InlineData("Ana", "Gómez", "", "1")]
    [InlineData("Ana", "Gómez", "a@a.com", "")]
    [InlineData("   ", "Gómez", "a@a.com", "1")]
    public async Task CreateLectorAsync_WithMissingOrBlankField_ThrowsValidationException(
        string nombre, string apellido, string email, string identificador)
    {
        var dto = new LectorCreateDTO { Nombre = nombre, Apellido = apellido, Email = email, Identificador = identificador };

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Theory]
    [InlineData("no-es-un-email")]
    [InlineData("falta-arroba.com")]
    [InlineData("sin-dominio@")]
    [InlineData("espacio en@email.com")]
    public async Task CreateLectorAsync_WithInvalidEmailFormat_ThrowsValidationException(string email)
    {
        var dto = NuevoLectorDto();
        dto.Email = email;

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLectorAsync_NormalizesEmailWithTrimAndLowerCase()
    {
        var dto = NuevoLectorDto();
        dto.Email = "  Ana@Example.COM ";
        SetupSinDuplicados();
        _lectorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector l, CancellationToken _) =>
            {
                l.Id = Guid.NewGuid();
                return l;
            });

        var service = CrearService();
        var resultado = await service.CreateAsync(dto);

        Assert.Equal("ana@example.com", resultado.Email);
    }

    [Fact]
    public async Task CreateLectorAsync_WithTooLongTitulo_ThrowsValidationException()
    {
        var dto = NuevoLectorDto();
        dto.Nombre = new string('A', 101);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLectorAsync_WhenIdentificadorAlreadyExists_ThrowsConflictException()
    {
        var dto = NuevoLectorDto();

        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(dto.Identificador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lector { Id = Guid.NewGuid(), Identificador = dto.Identificador });

        var service = CrearService();

        await Assert.ThrowsAsync<IdentificadorDuplicadoException>(() => service.CreateAsync(dto));
        _lectorRepository.Verify(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLectorAsync_WhenIdentificadorDuplicadoWithDifferentCaseAndSpaces_ThrowsConflictException()
    {
        var dto = NuevoLectorDto();
        dto.Identificador = "  l-4521  ";

        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync("L-4521", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lector { Id = Guid.NewGuid(), Identificador = "L-4521" });

        var service = CrearService();

        await Assert.ThrowsAsync<IdentificadorDuplicadoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLectorAsync_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        var dto = NuevoLectorDto();
        dto.Email = " ANA@EXAMPLE.COM ";

        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(dto.Identificador, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector?)null);
        _lectorRepository
            .Setup(r => r.GetByEmailAsync("ana@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lector { Id = Guid.NewGuid(), Email = "ana@example.com" });

        var service = CrearService();

        await Assert.ThrowsAsync<LectorEmailDuplicadoException>(() => service.CreateAsync(dto));
        _lectorRepository.Verify(r => r.CreateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLectorByIdAsync_WhenLectorDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Lector?)null);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorNotFoundException>(() => service.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteLectorAsync_WithoutLoanHistory_DeletesLector()
    {
        var id = Guid.NewGuid();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Identificador = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.TienePrestamosPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CrearService();
        await service.DeleteAsync(id);

        _lectorRepository.Verify(r => r.DeleteAsync(lector, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLectorAsync_WhenLectorHasLoanHistory_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Identificador = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.TienePrestamosPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorConHistorialPrestamosException>(() => service.DeleteAsync(id));
        _lectorRepository.Verify(r => r.DeleteAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLectorAsync_WithValidData_UpdatesLector()
    {
        var id = Guid.NewGuid();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Identificador = "30111222" };
        var dto = new LectorUpdateDTO { Nombre = "Ana María", Apellido = "Gómez", Email = "anamaria@example.com", Identificador = "30111222" };
        _lectorRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);
        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(dto.Identificador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);
        _lectorRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector?)null);

        var service = CrearService();
        var resultado = await service.UpdateAsync(id, dto);

        Assert.Equal("Ana María", resultado.Nombre);
        Assert.Equal("anamaria@example.com", resultado.Email);
        _lectorRepository.Verify(r => r.UpdateAsync(lector, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLectorAsync_WhenIdentificadorBelongsToOtherLector_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Identificador = "30111222" };
        var otro = new Lector { Id = Guid.NewGuid(), Nombre = "Luis", Apellido = "Paz", Email = "luis@example.com", Identificador = "40999888" };
        var dto = new LectorUpdateDTO { Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Identificador = "40999888" };
        _lectorRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);
        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(dto.Identificador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otro);

        var service = CrearService();

        await Assert.ThrowsAsync<IdentificadorDuplicadoException>(() => service.UpdateAsync(id, dto));
        _lectorRepository.Verify(r => r.UpdateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLectorAsync_WhenEmailBelongsToOtherLector_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "ana@example.com", Identificador = "30111222" };
        var otro = new Lector { Id = Guid.NewGuid(), Nombre = "Luis", Apellido = "Paz", Email = "luis@example.com", Identificador = "40999888" };
        var dto = new LectorUpdateDTO { Nombre = "Ana", Apellido = "Gómez", Email = "luis@example.com", Identificador = "30111222" };
        _lectorRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);
        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(dto.Identificador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lector);
        _lectorRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otro);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorEmailDuplicadoException>(() => service.UpdateAsync(id, dto));
        _lectorRepository.Verify(r => r.UpdateAsync(It.IsAny<Lector>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
