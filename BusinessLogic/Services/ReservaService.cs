using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Reservas;
using BiblioGest.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.BusinessLogic.Services;

public class ReservaService : IReservaService
{
    private readonly IReservaRepository _reservaRepository;
    private readonly ILibroRepository _libroRepository;
    private readonly ILectorRepository _lectorRepository;
    private readonly IColaDeReservas _colaDeReservas;
    private readonly IUnitOfWork _unitOfWork;

    public ReservaService(
        IReservaRepository reservaRepository,
        ILibroRepository libroRepository,
        ILectorRepository lectorRepository,
        IColaDeReservas colaDeReservas,
        IUnitOfWork unitOfWork)
    {
        _reservaRepository = reservaRepository;
        _libroRepository = libroRepository;
        _lectorRepository = lectorRepository;
        _colaDeReservas = colaDeReservas;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ReservaResponseDTO>> GetColaPorLibroAsync(int libroId, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(libroId, ct) ?? throw new LibroNotFoundException(libroId);
        await _colaDeReservas.ProcesarAsignacionesVencidasAsync(libro, ct);

        var activas = await _reservaRepository.GetActivasPorLibroAsync(libroId, ct);
        return activas.Select(r => MapToResponseDTO(r, activas)).ToList();
    }

    public async Task<ReservaResponseDTO> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var reserva = await _reservaRepository.GetByIdAsync(id, ct) ?? throw new ReservaNotFoundException(id);

        var libro = await _libroRepository.GetByIdAsync(reserva.LibroId, ct);
        if (libro is not null)
        {
            await _colaDeReservas.ProcesarAsignacionesVencidasAsync(libro, ct);
            reserva = await _reservaRepository.GetByIdAsync(id, ct) ?? throw new ReservaNotFoundException(id);
        }

        var activas = await _reservaRepository.GetActivasPorLibroAsync(reserva.LibroId, ct);
        return MapToResponseDTO(reserva, activas);
    }

    public async Task<ReservaResponseDTO> CreateAsync(ReservaCreateDTO dto, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(dto.LectorId, ct)
            ?? throw new LectorNotFoundException(dto.LectorId);
        var libro = await _libroRepository.GetByIdAsync(dto.LibroId, ct)
            ?? throw new LibroNotFoundException(dto.LibroId);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _colaDeReservas.ProcesarAsignacionesVencidasAsync(libro, ct);

            if (libro.Stock > 0)
            {
                throw new ReservaConStockDisponibleException(libro.Id, libro.Stock);
            }

            if (await _reservaRepository.ExisteActivaAsync(lector.Id, libro.Id, ct))
            {
                throw new ReservaDuplicadaException(lector.Id, libro.Id);
            }

            var reserva = new Reserva
            {
                LectorId = lector.Id,
                LibroId = libro.Id,
                FechaReserva = DateTime.UtcNow,
                Estado = EstadoReserva.EnCola
            };

            var creada = await _reservaRepository.CreateAsync(reserva, ct);
            await _unitOfWork.CommitAsync(ct);

            creada.Lector = lector;
            creada.Libro = libro;

            var activas = await _reservaRepository.GetActivasPorLibroAsync(libro.Id, ct);
            return MapToResponseDTO(creada, activas);
        }
        catch (DbUpdateConcurrencyException)
        {
            await _unitOfWork.RollbackAsync(ct);
            throw new LibroConcurrenciaException(libro.Id);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ReservaResponseDTO> CancelarAsync(int id, CancellationToken ct = default)
    {
        var reserva = await _reservaRepository.GetByIdAsync(id, ct) ?? throw new ReservaNotFoundException(id);

        if (reserva.Estado != EstadoReserva.EnCola && reserva.Estado != EstadoReserva.Asignada)
        {
            throw new ReservaNoActivaException(id, reserva.Estado.ToString());
        }

        var teniaEjemplarAsignado = reserva.Estado == EstadoReserva.Asignada;
        reserva.Estado = EstadoReserva.Cancelada;
        await _reservaRepository.UpdateAsync(reserva, ct);

        if (teniaEjemplarAsignado)
        {
            var libro = await _libroRepository.GetByIdAsync(reserva.LibroId, ct)
                ?? throw new LibroNotFoundException(reserva.LibroId);
            await _colaDeReservas.LiberarEjemplarAsync(libro, DateTime.UtcNow, ct);
        }

        return MapToResponseDTO(reserva, []);
    }

    private static int? CalcularPosicionEnCola(Reserva reserva, IReadOnlyList<Reserva> activas)
    {
        if (reserva.Estado != EstadoReserva.EnCola)
        {
            return null;
        }

        return activas.Count(r => r.Estado == EstadoReserva.EnCola && r.Id < reserva.Id) + 1;
    }

    private static ReservaResponseDTO MapToResponseDTO(Reserva reserva, IReadOnlyList<Reserva> activas) => new()
    {
        Id = reserva.Id,
        LectorId = reserva.LectorId,
        LectorNombreCompleto = reserva.Lector is null
            ? string.Empty
            : $"{reserva.Lector.Nombre} {reserva.Lector.Apellido}",
        LibroId = reserva.LibroId,
        LibroTitulo = reserva.Libro?.Titulo ?? string.Empty,
        FechaReserva = reserva.FechaReserva,
        Estado = reserva.Estado.ToString(),
        PosicionEnCola = CalcularPosicionEnCola(reserva, activas),
        FechaAsignacion = reserva.FechaAsignacion,
        FechaLimiteRetiro = reserva.FechaLimiteRetiro
    };
}
