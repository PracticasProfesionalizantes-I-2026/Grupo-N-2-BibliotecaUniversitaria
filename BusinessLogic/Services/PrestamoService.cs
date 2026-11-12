using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.BusinessLogic.Services;

public class PrestamoService : IPrestamoService
{
    private const int MaximoPrestamosActivosPorLector = 3;
    private const int DiasDePrestamo = 14;
    private const int DiasDeSuspensionPorDiaDeAtraso = 1;
    private const int MaximoRenovaciones = 2;

    private readonly IPrestamoRepository _prestamoRepository;
    private readonly ILibroRepository _libroRepository;
    private readonly ILectorRepository _lectorRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IColaDeReservas _colaDeReservas;
    private readonly IUnitOfWork _unitOfWork;

    public PrestamoService(
        IPrestamoRepository prestamoRepository,
        ILibroRepository libroRepository,
        ILectorRepository lectorRepository,
        IReservaRepository reservaRepository,
        IColaDeReservas colaDeReservas,
        IUnitOfWork unitOfWork)
    {
        _prestamoRepository = prestamoRepository;
        _libroRepository = libroRepository;
        _lectorRepository = lectorRepository;
        _reservaRepository = reservaRepository;
        _colaDeReservas = colaDeReservas;
        _unitOfWork = unitOfWork;
    }

    public async Task<PrestamoResponseDTO> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);
        return MapToResponseDTO(prestamo);
    }

    public async Task<PrestamoResponseDTO> CreateAsync(PrestamoCreateDTO dto, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(dto.LectorId, ct)
            ?? throw new LectorNotFoundException(dto.LectorId);
        var libro = await _libroRepository.GetByIdAsync(dto.LibroId, ct)
            ?? throw new LibroNotFoundException(dto.LibroId);

        // Transacción explícita para que la lectura de Stock y su descuento
        // sean atómicos: si otra petición concurrente ya modificó el libro,
        // el RowVersion leído queda desactualizado y el UPDATE de abajo
        // lanza DbUpdateConcurrencyException en vez de pisar el stock.
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _colaDeReservas.ProcesarAsignacionesVencidasAsync(libro, ct);

            var bloqueo = await BuscarBloqueoDelLectorAsync(lector, ct);
            if (bloqueo is not null)
            {
                throw bloqueo;
            }

            if (await _prestamoRepository.ExistePrestamoActivoAsync(lector.Id, libro.Id, ct))
            {
                throw new PrestamoActivoDuplicadoException(lector.Id, libro.Id);
            }

            var reservaAsignada = await _reservaRepository.GetAsignadaAsync(lector.Id, libro.Id, ct);
            if (reservaAsignada is null && libro.Stock <= 0)
            {
                throw new StockInsuficienteException(dto.LibroId);
            }

            var fechaPrestamo = DateTime.UtcNow;
            var prestamo = new Prestamo
            {
                LectorId = lector.Id,
                LibroId = libro.Id,
                FechaPrestamo = fechaPrestamo,
                FechaVencimiento = fechaPrestamo.AddDays(DiasDePrestamo),
                Estado = EstadoPrestamo.Activo
            };

            var creado = await _prestamoRepository.CreateAsync(prestamo, ct);

            if (reservaAsignada is not null)
            {
                reservaAsignada.Estado = EstadoReserva.Concretada;
                await _reservaRepository.UpdateAsync(reservaAsignada, ct);
            }
            else
            {
                libro.Stock -= 1;
                await _libroRepository.UpdateAsync(libro, ct);
            }

            await _unitOfWork.CommitAsync(ct);

            creado.Lector = lector;
            creado.Libro = libro;
            return MapToResponseDTO(creado);
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

    public async Task<IReadOnlyList<PrestamoResponseDTO>> GetActivosAsync(CancellationToken ct = default)
    {
        var activos = await _prestamoRepository.GetActivosAsync(ct);
        return activos.Select(p => MapToResponseDTO(p)).ToList();
    }

    public async Task<EstadoLectorPrestamoDTO> GetEstadoLectorAsync(int lectorId, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(lectorId, ct) ?? throw new LectorNotFoundException(lectorId);

        var bloqueo = await BuscarBloqueoDelLectorAsync(lector, ct);
        var ahora = DateTime.UtcNow;

        return new EstadoLectorPrestamoDTO
        {
            PrestamosActivos = await _prestamoRepository.CountActivosPorLectorAsync(lectorId, ct),
            MaximoPrestamos = MaximoPrestamosActivosPorLector,
            EnMora = await _prestamoRepository.LectorTieneMoraAsync(lectorId, ct),
            TieneLibroExtraviado = await _prestamoRepository.LectorTieneExtraviadosAsync(lectorId, ct),
            SuspendidoHasta = EstaSuspendido(lector, ahora) ? lector.SuspendidoHasta : null,
            PuedePedir = bloqueo is null,
            Motivo = bloqueo?.Message,
            FechaPrestamo = ahora,
            FechaVencimiento = ahora.AddDays(DiasDePrestamo)
        };
    }

    private async Task<ForbiddenException?> BuscarBloqueoDelLectorAsync(Lector lector, CancellationToken ct)
    {
        var prestamosActivos = await _prestamoRepository.CountActivosPorLectorAsync(lector.Id, ct);
        if (prestamosActivos >= MaximoPrestamosActivosPorLector)
        {
            return new LimitePrestamosActivosException(lector.Id);
        }

        if (EstaSuspendido(lector, DateTime.UtcNow))
        {
            return new LectorSuspendidoException(lector.Id, lector.SuspendidoHasta!.Value);
        }

        if (await _prestamoRepository.LectorTieneExtraviadosAsync(lector.Id, ct))
        {
            return new LectorConLibroExtraviadoException(lector.Id);
        }

        if (await _prestamoRepository.LectorTieneMoraAsync(lector.Id, ct))
        {
            return new LectorEnMoraException(lector.Id);
        }

        return null;
    }

    private static bool EstaSuspendido(Lector lector, DateTime ahora) =>
        lector.SuspendidoHasta.HasValue && lector.SuspendidoHasta.Value > ahora;

    public async Task<PrestamoResponseDTO> RegistrarDevolucionAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);

        if (prestamo.Estado == EstadoPrestamo.Devuelto)
        {
            throw new PrestamoYaDevueltoException(id);
        }

        if (prestamo.Estado != EstadoPrestamo.Activo)
        {
            throw new PrestamoNoActivoException(id, prestamo.Estado.ToString());
        }

        await CerrarPrestamoAsync(prestamo, EstadoPrestamo.Devuelto, ct);
        return MapToResponseDTO(prestamo);
    }

    public async Task<PrestamoResponseDTO> RegistrarExtravioAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);

        if (prestamo.Estado != EstadoPrestamo.Activo)
        {
            throw new PrestamoNoActivoException(id, prestamo.Estado.ToString());
        }

        prestamo.Estado = EstadoPrestamo.Extraviado;
        await _prestamoRepository.UpdateAsync(prestamo, ct);
        return MapToResponseDTO(prestamo);
    }

    public async Task<PrestamoResponseDTO> RegistrarReposicionAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);

        if (prestamo.Estado != EstadoPrestamo.Extraviado)
        {
            throw new PrestamoNoExtraviadoException(id, prestamo.Estado.ToString());
        }

        await CerrarPrestamoAsync(prestamo, EstadoPrestamo.Repuesto, ct);
        return MapToResponseDTO(prestamo);
    }

    private async Task CerrarPrestamoAsync(Prestamo prestamo, EstadoPrestamo estadoFinal, CancellationToken ct)
    {
        // El préstamo, la reserva/stock del libro y la suspensión del lector
        // se marcan en memoria sin guardar cada uno por separado, y se
        // persisten juntos con un único SaveChanges (atómico por sí mismo:
        // si falla, no queda nada guardado a mitad de camino).
        prestamo.Estado = estadoFinal;
        prestamo.FechaDevolucion = DateTime.UtcNow;
        await _prestamoRepository.UpdateAsync(prestamo, ct, guardarCambios: false);

        if (prestamo.Libro is not null &&
            !await _colaDeReservas.AsignarAlSiguienteAsync(prestamo.Libro, prestamo.FechaDevolucion.Value, ct, guardarCambios: false))
        {
            prestamo.Libro.Stock += 1;
            await _libroRepository.UpdateAsync(prestamo.Libro, ct, guardarCambios: false);
        }

        await AplicarSuspensionPorAtrasoAsync(prestamo, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LibroConcurrenciaException(prestamo.LibroId);
        }
    }

    public async Task<PrestamoResponseDTO> RenovarAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);

        if (prestamo.Estado != EstadoPrestamo.Activo)
        {
            throw new PrestamoNoActivoException(id, prestamo.Estado.ToString());
        }

        if (prestamo.FechaVencimiento < DateTime.UtcNow)
        {
            throw new PrestamoVencidoNoRenovableException(id);
        }

        if (prestamo.Renovaciones >= MaximoRenovaciones)
        {
            throw new LimiteRenovacionesException(id, MaximoRenovaciones);
        }

        if (prestamo.Libro is not null)
        {
            await _colaDeReservas.ProcesarAsignacionesVencidasAsync(prestamo.Libro, ct);
        }

        if (await _reservaRepository.TieneActivasPorLibroAsync(prestamo.LibroId, ct))
        {
            throw new RenovacionConReservasException(id);
        }

        prestamo.FechaVencimiento = prestamo.FechaVencimiento.AddDays(DiasDePrestamo);
        prestamo.Renovaciones += 1;
        await _prestamoRepository.UpdateAsync(prestamo, ct);
        return MapToResponseDTO(prestamo);
    }

    private async Task AplicarSuspensionPorAtrasoAsync(Prestamo prestamo, CancellationToken ct)
    {
        var diasDeAtraso = CalcularDiasDeAtraso(prestamo.FechaVencimiento, prestamo.FechaDevolucion!.Value);
        if (diasDeAtraso == 0)
        {
            return;
        }

        var lector = prestamo.Lector
            ?? await _lectorRepository.GetByIdAsync(prestamo.LectorId, ct)
            ?? throw new LectorNotFoundException(prestamo.LectorId);

        var desde = lector.SuspendidoHasta.HasValue && lector.SuspendidoHasta.Value > prestamo.FechaDevolucion.Value
            ? lector.SuspendidoHasta.Value
            : prestamo.FechaDevolucion.Value;

        lector.SuspendidoHasta = desde.AddDays(diasDeAtraso * DiasDeSuspensionPorDiaDeAtraso);
        await _lectorRepository.UpdateAsync(lector, ct, guardarCambios: false);
    }

    private static int CalcularDiasDeAtraso(DateTime fechaVencimiento, DateTime fechaDevolucion)
    {
        var dias = (fechaDevolucion.Date - fechaVencimiento.Date).Days;
        return Math.Max(dias, 0);
    }

    public async Task<IReadOnlyList<PrestamoResponseDTO>> GetVencidosAsync(CancellationToken ct = default)
    {
        var vencidos = await _prestamoRepository.GetVencidosAsync(ct);
        return vencidos.Select(p => MapToResponseDTO(p, enMora: true)).ToList();
    }

    private static PrestamoResponseDTO MapToResponseDTO(Prestamo prestamo, bool? enMora = null) => new()
    {
        Id = prestamo.Id,
        LectorId = prestamo.LectorId,
        LectorNombreCompleto = prestamo.Lector is null
            ? string.Empty
            : $"{prestamo.Lector.Nombre} {prestamo.Lector.Apellido}",
        LibroId = prestamo.LibroId,
        LibroTitulo = prestamo.Libro?.Titulo ?? string.Empty,
        FechaPrestamo = prestamo.FechaPrestamo,
        FechaVencimiento = prestamo.FechaVencimiento,
        FechaDevolucion = prestamo.FechaDevolucion,
        Renovaciones = prestamo.Renovaciones,
        Estado = prestamo.Estado.ToString(),
        EnMora = enMora ?? (prestamo.Estado == EstadoPrestamo.Activo && prestamo.FechaVencimiento < DateTime.UtcNow)
    };
}
