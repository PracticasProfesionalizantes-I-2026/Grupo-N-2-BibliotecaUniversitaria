using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using Moq;
using Xunit;

namespace BiblioGest.UnitTests;

public class ColaDeReservasTests
{
    private readonly Mock<IReservaRepository> _reservaRepository = new();
    private readonly Mock<ILibroRepository> _libroRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ColaDeReservas CrearCola() => new(_reservaRepository.Object, _libroRepository.Object, _unitOfWork.Object);

    private static Libro CrearLibro(int stock) =>
        new() { Id = IdsDePrueba.Nuevo(), Titulo = "Rayuela", Autor = "Cortázar", Isbn = "1", Ubicacion = "A1-1", Stock = stock };

    private static Reserva CrearReserva(Libro libro, EstadoReserva estado, DateTime? fechaLimiteRetiro = null) => new()
    {
        Id = IdsDePrueba.Nuevo(),
        LectorId = IdsDePrueba.Nuevo(),
        LibroId = libro.Id,
        FechaReserva = DateTime.UtcNow.AddDays(-3),
        Estado = estado,
        FechaAsignacion = fechaLimiteRetiro?.AddHours(-ColaDeReservas.HorasParaRetirar),
        FechaLimiteRetiro = fechaLimiteRetiro
    };

    private void ConfigurarCola(Libro libro, params Reserva[] reservas) =>
        _reservaRepository
            .Setup(r => r.GetActivasPorLibroAsync(libro.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservas.ToList());

    [Fact]
    public async Task AsignarAlSiguienteAsync_WithReaderInQueue_AssignsFor24Hours()
    {
        var libro = CrearLibro(stock: 0);
        var primera = CrearReserva(libro, EstadoReserva.EnCola);
        var segunda = CrearReserva(libro, EstadoReserva.EnCola);
        ConfigurarCola(libro, primera, segunda);
        var desde = DateTime.UtcNow;

        var asignada = await CrearCola().AsignarAlSiguienteAsync(libro, desde);

        Assert.True(asignada);
        Assert.Equal(EstadoReserva.Asignada, primera.Estado);
        Assert.Equal(desde, primera.FechaAsignacion);
        Assert.Equal(desde.AddHours(24), primera.FechaLimiteRetiro);
        Assert.Equal(EstadoReserva.EnCola, segunda.Estado);
        _reservaRepository.Verify(r => r.UpdateAsync(primera, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AsignarAlSiguienteAsync_WithEmptyQueue_ReturnsFalse()
    {
        var libro = CrearLibro(stock: 0);
        ConfigurarCola(libro);

        var asignada = await CrearCola().AsignarAlSiguienteAsync(libro, DateTime.UtcNow);

        Assert.False(asignada);
    }

    [Fact]
    public async Task ProcesarAsignacionesVencidasAsync_WhenAssignmentExpired_PassesCopyToNextInQueue()
    {
        var libro = CrearLibro(stock: 0);
        var limiteVencido = DateTime.UtcNow.AddHours(-2);
        var vencida = CrearReserva(libro, EstadoReserva.Asignada, limiteVencido);
        var siguiente = CrearReserva(libro, EstadoReserva.EnCola);
        ConfigurarCola(libro, vencida, siguiente);

        await CrearCola().ProcesarAsignacionesVencidasAsync(libro);

        Assert.Equal(EstadoReserva.Vencida, vencida.Estado);
        Assert.Equal(EstadoReserva.Asignada, siguiente.Estado);
        Assert.Equal(limiteVencido, siguiente.FechaAsignacion);
        Assert.Equal(limiteVencido.AddHours(24), siguiente.FechaLimiteRetiro);
        Assert.Equal(0, libro.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(It.IsAny<Libro>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
        // Las dos reservas se marcan en memoria sin guardar cada una por separado...
        _reservaRepository.Verify(r => r.UpdateAsync(It.IsAny<Reserva>(), It.IsAny<CancellationToken>(), false), Times.Exactly(2));
        // ...y se persiste todo junto con un único SaveChanges al final.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsignacionesVencidasAsync_WhenAssignmentExpiredAndQueueEmpty_ReturnsCopyToStock()
    {
        var libro = CrearLibro(stock: 0);
        var vencida = CrearReserva(libro, EstadoReserva.Asignada, DateTime.UtcNow.AddMinutes(-1));
        ConfigurarCola(libro, vencida);

        await CrearCola().ProcesarAsignacionesVencidasAsync(libro);

        Assert.Equal(EstadoReserva.Vencida, vencida.Estado);
        Assert.Equal(1, libro.Stock);
        _libroRepository.Verify(r => r.UpdateAsync(libro, It.IsAny<CancellationToken>(), false), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsignacionesVencidasAsync_WhenNextAssignmentAlsoExpired_ChainsUntilStock()
    {
        var libro = CrearLibro(stock: 0);
        var vencida = CrearReserva(libro, EstadoReserva.Asignada, DateTime.UtcNow.AddHours(-30));
        var siguiente = CrearReserva(libro, EstadoReserva.EnCola);
        ConfigurarCola(libro, vencida, siguiente);

        await CrearCola().ProcesarAsignacionesVencidasAsync(libro);

        Assert.Equal(EstadoReserva.Vencida, vencida.Estado);
        Assert.Equal(EstadoReserva.Vencida, siguiente.Estado);
        Assert.Equal(1, libro.Stock);
        // Toda la cascada (dos vencimientos + el incremento de stock) se
        // persiste con un único SaveChanges, no uno por paso.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsignacionesVencidasAsync_WhenAssignmentStillValid_DoesNothing()
    {
        var libro = CrearLibro(stock: 0);
        var vigente = CrearReserva(libro, EstadoReserva.Asignada, DateTime.UtcNow.AddHours(5));
        ConfigurarCola(libro, vigente);

        await CrearCola().ProcesarAsignacionesVencidasAsync(libro);

        Assert.Equal(EstadoReserva.Asignada, vigente.Estado);
        Assert.Equal(0, libro.Stock);
        _reservaRepository.Verify(r => r.UpdateAsync(It.IsAny<Reserva>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
