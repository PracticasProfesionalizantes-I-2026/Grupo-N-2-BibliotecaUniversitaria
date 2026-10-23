using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Validation;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class LectorService : ILectorService
{
    private const int NombreMaxLength = 100;
    private const int ApellidoMaxLength = 100;
    private const int EmailMaxLength = 150;
    private const int IdentificadorMaxLength = 20;

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
        var (email, identificador) = ValidarDatos(dto.Nombre, dto.Apellido, dto.Email, dto.Identificador);
        await ValidarIdentificadorUnicoAsync(identificador, lectorIdActual: null, ct);
        await ValidarEmailUnicoAsync(email, lectorIdActual: null, ct);

        var lector = new Lector
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = email,
            Identificador = identificador
        };

        var creado = await _lectorRepository.CreateAsync(lector, ct);
        return MapToResponseDTO(creado);
    }

    public async Task<LectorResponseDTO> UpdateAsync(Guid id, LectorUpdateDTO dto, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);

        var (email, identificador) = ValidarDatos(dto.Nombre, dto.Apellido, dto.Email, dto.Identificador);
        await ValidarIdentificadorUnicoAsync(identificador, lectorIdActual: id, ct);
        await ValidarEmailUnicoAsync(email, lectorIdActual: id, ct);

        lector.Nombre = dto.Nombre.Trim();
        lector.Apellido = dto.Apellido.Trim();
        lector.Email = email;
        lector.Identificador = identificador;

        await _lectorRepository.UpdateAsync(lector, ct);
        return MapToResponseDTO(lector);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);

        if (await _prestamoRepository.TienePrestamosPorLectorAsync(id, ct))
        {
            throw new LectorConHistorialPrestamosException(id);
        }

        await _lectorRepository.DeleteAsync(lector, ct);
    }

    private static (string Email, string Identificador) ValidarDatos(string nombre, string apellido, string email, string identificador)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(identificador))
        {
            throw new LectorInvalidoException(
                "Nombre, apellido, email e identificador (DNI o legajo) son obligatorios.");
        }

        if (nombre.Trim().Length > NombreMaxLength)
        {
            throw new LectorInvalidoException($"El nombre no puede superar los {NombreMaxLength} caracteres.");
        }

        if (apellido.Trim().Length > ApellidoMaxLength)
        {
            throw new LectorInvalidoException($"El apellido no puede superar los {ApellidoMaxLength} caracteres.");
        }

        var emailNormalizado = email.Trim().ToLowerInvariant();
        if (emailNormalizado.Length > EmailMaxLength)
        {
            throw new LectorInvalidoException($"El email no puede superar los {EmailMaxLength} caracteres.");
        }

        if (!EmailValidator.EsFormatoValido(emailNormalizado))
        {
            throw new LectorInvalidoException("El email no tiene un formato válido.");
        }

        var identificadorNormalizado = identificador.Trim().ToUpperInvariant();
        if (identificadorNormalizado.Length > IdentificadorMaxLength)
        {
            throw new LectorInvalidoException($"El identificador no puede superar los {IdentificadorMaxLength} caracteres.");
        }

        return (emailNormalizado, identificadorNormalizado);
    }

    private async Task ValidarIdentificadorUnicoAsync(string identificador, Guid? lectorIdActual, CancellationToken ct)
    {
        var existente = await _lectorRepository.GetByIdentificadorAsync(identificador, ct);
        if (existente is not null && existente.Id != lectorIdActual)
        {
            throw new IdentificadorDuplicadoException(identificador);
        }
    }

    private async Task ValidarEmailUnicoAsync(string email, Guid? lectorIdActual, CancellationToken ct)
    {
        var existente = await _lectorRepository.GetByEmailAsync(email, ct);
        if (existente is not null && existente.Id != lectorIdActual)
        {
            throw new LectorEmailDuplicadoException(email);
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
