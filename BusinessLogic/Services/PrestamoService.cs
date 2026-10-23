using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Mapping;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class PrestamoService : IPrestamoService
{
    private const int MaximoPrestamosActivosPorLector = 3;
    private const int DiasDePrestamo = 14;

    private readonly IPrestamoRepository _prestamoRepository;
    private readonly ILibroRepository _libroRepository;
    private readonly ILectorRepository _lectorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PrestamoService(
        IPrestamoRepository prestamoRepository,
        ILibroRepository libroRepository,
        ILectorRepository lectorRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _prestamoRepository = prestamoRepository;
        _libroRepository = libroRepository;
        _lectorRepository = lectorRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PrestamoResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);
        return PrestamoMapper.ToResponseDTO(prestamo, AhoraUtc());
    }

    public async Task<PrestamoResponseDTO> CreateAsync(PrestamoCreateDTO dto, CancellationToken ct = default)
    {
        if (dto.LectorId == Guid.Empty || dto.LibroId == Guid.Empty)
        {
            throw new PrestamoInvalidoException("LectorId y LibroId son obligatorios.");
        }

        var lector = await _lectorRepository.GetByIdAsync(dto.LectorId, ct)
            ?? throw new LectorNotFoundException(dto.LectorId);
        var libro = await _libroRepository.GetByIdAsync(dto.LibroId, ct)
            ?? throw new LibroNotFoundException(dto.LibroId);

        var prestamosActivos = await _prestamoRepository.CountActivosPorLectorAsync(dto.LectorId, ct);
        if (prestamosActivos >= MaximoPrestamosActivosPorLector)
        {
            throw new LimitePrestamosActivosException(dto.LectorId);
        }

        var ahoraUtc = AhoraUtc();

        if (await _prestamoRepository.LectorTieneMoraAsync(dto.LectorId, ahoraUtc, ct))
        {
            throw new LectorEnMoraException(dto.LectorId);
        }

        if (await _prestamoRepository.ExistePrestamoActivoAsync(dto.LectorId, dto.LibroId, ct))
        {
            throw new PrestamoActivoDuplicadoException(dto.LectorId, dto.LibroId);
        }

        if (libro.Stock <= 0)
        {
            throw new StockInsuficienteException(dto.LibroId);
        }

        var prestamo = new Prestamo
        {
            LectorId = lector.Id,
            LibroId = libro.Id,
            FechaPrestamo = ahoraUtc,
            FechaVencimiento = ahoraUtc.AddDays(DiasDePrestamo),
            Estado = EstadoPrestamo.Activo
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        var creado = await _prestamoRepository.CreateAsync(prestamo, ct);

        libro.Stock -= 1;
        await _libroRepository.UpdateAsync(libro, ct);

        await transaction.CommitAsync(ct);

        creado.Lector = lector;
        creado.Libro = libro;
        return PrestamoMapper.ToResponseDTO(creado, ahoraUtc);
    }

    public async Task<PrestamoResponseDTO> RegistrarDevolucionAsync(Guid id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);

        if (prestamo.Estado == EstadoPrestamo.Devuelto)
        {
            throw new PrestamoYaDevueltoException(id);
        }

        var ahoraUtc = AhoraUtc();

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        prestamo.Estado = EstadoPrestamo.Devuelto;
        prestamo.FechaDevolucion = ahoraUtc;
        await _prestamoRepository.UpdateAsync(prestamo, ct);

        if (prestamo.Libro is not null)
        {
            prestamo.Libro.Stock += 1;
            await _libroRepository.UpdateAsync(prestamo.Libro, ct);
        }

        await transaction.CommitAsync(ct);

        return PrestamoMapper.ToResponseDTO(prestamo, ahoraUtc);
    }

    public async Task<IReadOnlyList<PrestamoResponseDTO>> GetVencidosAsync(CancellationToken ct = default)
    {
        var ahoraUtc = AhoraUtc();
        var vencidos = await _prestamoRepository.GetVencidosAsync(ahoraUtc, ct);
        return vencidos.Select(p => PrestamoMapper.ToResponseDTO(p, ahoraUtc)).ToList();
    }

    private DateTime AhoraUtc() => _timeProvider.GetUtcNow().UtcDateTime;
}
