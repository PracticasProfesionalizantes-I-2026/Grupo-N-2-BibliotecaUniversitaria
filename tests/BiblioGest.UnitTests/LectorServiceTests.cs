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

    [Fact]
    public async Task CreateLectorAsync_WithValidData_SavesAndReturnsCreatedLector()
    {
        var dto = new LectorCreateDTO
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@example.com",
            Identificador = "30111222"
        };

        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(dto.Identificador, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lector?)null);
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

    [Fact]
    public async Task CreateLectorAsync_WithMissingRequiredField_ThrowsValidationException()
    {
        var dto = new LectorCreateDTO { Nombre = "", Apellido = "Gómez", Email = "a@a.com", Identificador = "1" };

        var service = CrearService();

        await Assert.ThrowsAsync<LectorInvalidoException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateLectorAsync_WhenIdentificadorAlreadyExists_ThrowsConflictException()
    {
        var dto = new LectorCreateDTO
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@example.com",
            Identificador = "30111222"
        };

        _lectorRepository
            .Setup(r => r.GetByIdentificadorAsync(dto.Identificador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lector { Id = Guid.NewGuid(), Identificador = dto.Identificador });

        var service = CrearService();

        await Assert.ThrowsAsync<IdentificadorDuplicadoException>(() => service.CreateAsync(dto));
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
    public async Task DeleteLectorAsync_WithoutActiveLoans_DeletesLector()
    {
        var id = Guid.NewGuid();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Identificador = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.TieneActivosPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CrearService();
        await service.DeleteAsync(id);

        _lectorRepository.Verify(r => r.DeleteAsync(lector, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLectorAsync_WhenLectorHasActiveLoans_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var lector = new Lector { Id = id, Nombre = "Ana", Apellido = "Gómez", Email = "a@a.com", Identificador = "1" };
        _lectorRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _prestamoRepository.Setup(r => r.TieneActivosPorLectorAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CrearService();

        await Assert.ThrowsAsync<LectorConPrestamosActivosException>(() => service.DeleteAsync(id));
    }
}
