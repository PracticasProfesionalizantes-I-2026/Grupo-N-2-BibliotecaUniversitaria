using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Reservas;
using BiblioGest.Shared.Exceptions;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class ReservaServiceTests
{
    private readonly Mock<IReservaRepository> _reservaRepository = new();
    private readonly Mock<ILibroRepository> _libroRepository = new();
    private readonly Mock<ILectorRepository> _lectorRepository = new();
    private readonly Mock<IColaDeReservas> _colaDeReservas = new();

    private ReservaService CrearService() =>
        new(_reservaRepository.Object, _libroRepository.Object, _lectorRepository.Object, _colaDeReservas.Object);

    private (Lector Lector, Libro Libro) ConfigurarLectorYLibro(int stock)
    {
        var lector = new Lector { Id = IdsDePrueba.Nuevo(), Nombre = "Bruno", Apellido = "Díaz" };
        var libro = new Libro { Id = IdsDePrueba.Nuevo(), Titulo = "Rayuela", Stock = stock };
        _lectorRepository.Setup(r => r.GetByIdAsync(lector.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lector);
        _libroRepository.Setup(r => r.GetByIdAsync(libro.Id, It.IsAny<CancellationToken>())).ReturnsAsync(libro);
        return (lector, libro);
    }

    private Reserva ConfigurarReserva(EstadoReserva estado)
    {
        var reserva = new Reserva
        {
            Id = IdsDePrueba.Nuevo(),
            LectorId = IdsDePrueba.Nuevo(),
            LibroId = IdsDePrueba.Nuevo(),
            FechaReserva = DateTime.UtcNow,
            Estado = estado
        };
        _reservaRepository.Setup(r => r.GetByIdAsync(reserva.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reserva);
        _libroRepository
            .Setup(r => r.GetByIdAsync(reserva.LibroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Libro { Id = reserva.LibroId, Stock = 0 });
        return reserva;
    }

    [Fact]
    public async Task CreateReservaAsync_WhenLibroHasNoStock_QueuesReservaWithPosition()
    {
        var (lector, libro) = ConfigurarLectorYLibro(stock: 0);
        var anterior = new Reserva { Id = IdsDePrueba.Nuevo(), LibroId = libro.Id, Estado = EstadoReserva.EnCola };
        var cola = new List<Reserva> { anterior };
        _reservaRepository
            .Setup(r => r.CreateAsync(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reserva r, CancellationToken _) =>
            {
                r.Id = IdsDePrueba.Nuevo();
                cola.Add(r);
                return r;
            });
        _reservaRepository
            .Setup(r => r.GetActivasPorLibroAsync(libro.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => cola);

        var resultado = await CrearService().CreateAsync(new ReservaCreateDTO { LectorId = lector.Id, LibroId = libro.Id });

        Assert.Equal("EnCola", resultado.Estado);
        Assert.Equal(2, resultado.PosicionEnCola);
        Assert.Equal("Bruno Díaz", resultado.LectorNombreCompleto);
        _colaDeReservas.Verify(c => c.ProcesarAsignacionesVencidasAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservaAsync_WhenLibroHasStock_ThrowsConflictException()
    {
        var (lector, libro) = ConfigurarLectorYLibro(stock: 2);

        await Assert.ThrowsAsync<ReservaConStockDisponibleException>(
            () => CrearService().CreateAsync(new ReservaCreateDTO { LectorId = lector.Id, LibroId = libro.Id }));
        _reservaRepository.Verify(r => r.CreateAsync(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReservaAsync_WhenLectorAlreadyHasActiveReserva_ThrowsConflictException()
    {
        var (lector, libro) = ConfigurarLectorYLibro(stock: 0);
        _reservaRepository
            .Setup(r => r.ExisteActivaAsync(lector.Id, libro.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ReservaDuplicadaException>(
            () => CrearService().CreateAsync(new ReservaCreateDTO { LectorId = lector.Id, LibroId = libro.Id }));
    }

    [Fact]
    public async Task CreateReservaAsync_WhenLectorDoesNotExist_ThrowsNotFoundException()
    {
        var (_, libro) = ConfigurarLectorYLibro(stock: 0);

        await Assert.ThrowsAsync<LectorNotFoundException>(
            () => CrearService().CreateAsync(new ReservaCreateDTO { LectorId = IdsDePrueba.Nuevo(), LibroId = libro.Id }));
    }

    [Fact]
    public async Task CancelarReservaAsync_WhenEnCola_CancelsWithoutReleasingCopy()
    {
        var reserva = ConfigurarReserva(EstadoReserva.EnCola);

        var resultado = await CrearService().CancelarAsync(reserva.Id);

        Assert.Equal("Cancelada", resultado.Estado);
        _colaDeReservas.Verify(
            c => c.LiberarEjemplarAsync(It.IsAny<Libro>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelarReservaAsync_WhenAsignada_ReleasesCopyToQueueOrStock()
    {
        var reserva = ConfigurarReserva(EstadoReserva.Asignada);

        var resultado = await CrearService().CancelarAsync(reserva.Id);

        Assert.Equal("Cancelada", resultado.Estado);
        _colaDeReservas.Verify(
            c => c.LiberarEjemplarAsync(It.Is<Libro>(l => l.Id == reserva.LibroId), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(EstadoReserva.Concretada)]
    [InlineData(EstadoReserva.Vencida)]
    [InlineData(EstadoReserva.Cancelada)]
    public async Task CancelarReservaAsync_WhenNotActive_ThrowsConflictException(EstadoReserva estado)
    {
        var reserva = ConfigurarReserva(estado);

        await Assert.ThrowsAsync<ReservaNoActivaException>(() => CrearService().CancelarAsync(reserva.Id));
    }

    [Fact]
    public async Task GetColaPorLibroAsync_WhenLibroDoesNotExist_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<LibroNotFoundException>(() => CrearService().GetColaPorLibroAsync(IdsDePrueba.Nuevo()));
    }
}
