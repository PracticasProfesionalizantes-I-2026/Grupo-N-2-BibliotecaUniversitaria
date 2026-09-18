using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class LectorService : ILectorService
{
    private readonly ILectorRepository _lectorRepository;
    private readonly IPrestamoRepository _prestamoRepository;

    public LectorService(ILectorRepository lectorRepository, IPrestamoRepository prestamoRepository)
    {
        _lectorRepository = lectorRepository;
        _prestamoRepository = prestamoRepository;
    }

    public async Task<IReadOnlyList<LectorResponseDTO>> GetAllAsync(CancellationToken ct = default)
    {
        var lectores = await _lectorRepository.GetAllAsync(ct);
        return lectores.Select(MapToResponseDTO).ToList();
    }

    public async Task<LectorResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);
        return MapToResponseDTO(lector);
    }

    public async Task<LectorResponseDTO> CreateAsync(LectorCreateDTO dto, CancellationToken ct = default)
    {
        ValidarDatos(dto.Nombre, dto.Apellido, dto.Email, dto.Identificador);
        await ValidarIdentificadorUnicoAsync(dto.Identificador, lectorIdActual: null, ct);

        var lector = new Lector
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = dto.Email.Trim(),
            Identificador = dto.Identificador.Trim()
        };

        var creado = await _lectorRepository.CreateAsync(lector, ct);
        return MapToResponseDTO(creado);
    }

    public async Task<LectorResponseDTO> UpdateAsync(Guid id, LectorUpdateDTO dto, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);

        ValidarDatos(dto.Nombre, dto.Apellido, dto.Email, dto.Identificador);
        await ValidarIdentificadorUnicoAsync(dto.Identificador, lectorIdActual: id, ct);

        lector.Nombre = dto.Nombre.Trim();
        lector.Apellido = dto.Apellido.Trim();
        lector.Email = dto.Email.Trim();
        lector.Identificador = dto.Identificador.Trim();

        await _lectorRepository.UpdateAsync(lector, ct);
        return MapToResponseDTO(lector);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);

        if (await _prestamoRepository.TieneActivosPorLectorAsync(id, ct))
        {
            throw new LectorConPrestamosActivosException(id);
        }

        await _lectorRepository.DeleteAsync(lector, ct);
    }

    private static void ValidarDatos(string nombre, string apellido, string email, string identificador)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(identificador))
        {
            throw new LectorInvalidoException(
                "Nombre, apellido, email e identificador (DNI o legajo) son obligatorios.");
        }
    }

    private async Task ValidarIdentificadorUnicoAsync(string identificador, Guid? lectorIdActual, CancellationToken ct)
    {
        var existente = await _lectorRepository.GetByIdentificadorAsync(identificador.Trim(), ct);
        if (existente is not null && existente.Id != lectorIdActual)
        {
            throw new IdentificadorDuplicadoException(identificador);
        }
    }

    private static LectorResponseDTO MapToResponseDTO(Lector lector) => new()
    {
        Id = lector.Id,
        Nombre = lector.Nombre,
        Apellido = lector.Apellido,
        Email = lector.Email,
        Identificador = lector.Identificador
    };
}
