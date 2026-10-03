using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Prestamos;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class PrestamoService : IPrestamoService
{
    private const int DiasDePrestamo = 14;

    private readonly IPrestamoRepository _prestamoRepository;
    private readonly ILibroRepository _libroRepository;
    private readonly ILectorRepository _lectorRepository;

    public PrestamoService(
        IPrestamoRepository prestamoRepository,
        ILibroRepository libroRepository,
        ILectorRepository lectorRepository)
    {
        _prestamoRepository = prestamoRepository;
        _libroRepository = libroRepository;
        _lectorRepository = lectorRepository;
    }

    public async Task<IReadOnlyList<PrestamoResponseDTO>> GetAllAsync(CancellationToken ct = default)
    {
        var prestamos = await _prestamoRepository.GetAllAsync(ct);
        return prestamos.Select(MapToResponseDTO).ToList();
    }

    public async Task<PrestamoResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default)
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

        creado.Lector = lector;
        creado.Libro = libro;
        return MapToResponseDTO(creado);
    }

    public async Task<PrestamoResponseDTO> RegistrarDevolucionAsync(Guid id, CancellationToken ct = default)
    {
        var prestamo = await _prestamoRepository.GetByIdAsync(id, ct) ?? throw new PrestamoNotFoundException(id);

        prestamo.Estado = EstadoPrestamo.Devuelto;
        prestamo.FechaDevolucion = DateTime.UtcNow;
        await _prestamoRepository.UpdateAsync(prestamo, ct);

        return MapToResponseDTO(prestamo);
    }

    private static PrestamoResponseDTO MapToResponseDTO(Prestamo prestamo) => new()
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
        Estado = prestamo.Estado.ToString()
    };
}
