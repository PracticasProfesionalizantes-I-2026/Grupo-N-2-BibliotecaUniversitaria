using System.Text.RegularExpressions;
using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Lectores;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class LectorService : ILectorService
{
    private const int LargoMinimoNombre = 2;
    private const int LargoMaximoNombre = 50;
    private const int LargoMinimoEmail = 6;
    private const int LargoMaximoEmail = 100;
    private static readonly Regex NombreRegex = new(@"^\p{L}+(?:[ '\-]\p{L}+)*$");
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex DniRegex = new(@"^\d{7,8}$");

    private readonly ILectorRepository _lectorRepository;
    private readonly IPrestamoRepository _prestamoRepository;
    private readonly IReservaRepository _reservaRepository;

    public LectorService(ILectorRepository lectorRepository, IPrestamoRepository prestamoRepository, IReservaRepository reservaRepository)
    {
        _lectorRepository = lectorRepository;
        _prestamoRepository = prestamoRepository;
        _reservaRepository = reservaRepository;
    }

    public async Task<IReadOnlyList<LectorResponseDTO>> GetAllAsync(CancellationToken ct = default)
    {
        var lectores = await _lectorRepository.GetAllAsync(ct);
        return lectores.Select(MapToResponseDTO).ToList();
    }

    public async Task<LectorResponseDTO> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);
        return MapToResponseDTO(lector);
    }

    public async Task<LectorResponseDTO> CreateAsync(LectorCreateDTO dto, CancellationToken ct = default)
    {
        ValidarDatos(dto.Nombre, dto.Apellido, dto.Email, dto.Dni);
        await ValidarDniUnicoAsync(dto.Dni, lectorIdActual: null, ct);

        var lector = new Lector
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = dto.Email.Trim(),
            Dni = dto.Dni.Trim()
        };

        var creado = await _lectorRepository.CreateAsync(lector, ct);
        return MapToResponseDTO(creado);
    }

    public async Task<LectorResponseDTO> UpdateAsync(int id, LectorUpdateDTO dto, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);

        ValidarDatos(dto.Nombre, dto.Apellido, dto.Email, dto.Dni);
        await ValidarDniUnicoAsync(dto.Dni, lectorIdActual: id, ct);

        lector.Nombre = dto.Nombre.Trim();
        lector.Apellido = dto.Apellido.Trim();
        lector.Email = dto.Email.Trim();
        lector.Dni = dto.Dni.Trim();

        await _lectorRepository.UpdateAsync(lector, ct);
        return MapToResponseDTO(lector);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var lector = await _lectorRepository.GetByIdAsync(id, ct) ?? throw new LectorNotFoundException(id);

        if (await _prestamoRepository.TieneActivosPorLectorAsync(id, ct))
        {
            throw new LectorConPrestamosActivosException(id);
        }

        if (await _reservaRepository.TieneActivasPorLectorAsync(id, ct))
        {
            throw new LectorConReservasActivasException(id);
        }

        if (await _prestamoRepository.TienePrestamosPorLectorAsync(id, ct))
        {
            throw new LectorConHistorialException(id);
        }

        await _lectorRepository.DeleteAsync(lector, ct);
    }

    private static void ValidarDatos(string nombre, string apellido, string email, string dni)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(dni))
        {
            throw new LectorInvalidoException(
                "Nombre, apellido, email y DNI son obligatorios.");
        }

        if (!LargoValido(nombre, LargoMinimoNombre, LargoMaximoNombre) ||
            !LargoValido(apellido, LargoMinimoNombre, LargoMaximoNombre))
        {
            throw new LectorInvalidoException(
                $"El nombre y el apellido deben tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres.");
        }

        if (!NombreRegex.IsMatch(nombre.Trim()) || !NombreRegex.IsMatch(apellido.Trim()))
        {
            throw new LectorInvalidoException(
                "El nombre y el apellido solo pueden tener letras, separadas por un único espacio, apóstrofo o guion (ejemplo: María José).");
        }

        if (!LargoValido(email, LargoMinimoEmail, LargoMaximoEmail))
        {
            throw new LectorInvalidoException(
                $"El email debe tener entre {LargoMinimoEmail} y {LargoMaximoEmail} caracteres.");
        }

        if (!EmailRegex.IsMatch(email.Trim()))
        {
            throw new LectorInvalidoException("El email no tiene un formato válido (ejemplo: nombre@dominio.com).");
        }

        if (!DniRegex.IsMatch(dni.Trim()))
        {
            throw new LectorInvalidoException(
                "El DNI debe tener 7 u 8 dígitos, sin puntos ni espacios (ejemplo: 30111222).");
        }
    }

    private static bool LargoValido(string valor, int minimo, int maximo)
    {
        var largo = valor.Trim().Length;
        return largo >= minimo && largo <= maximo;
    }

    private async Task ValidarDniUnicoAsync(string dni, int? lectorIdActual, CancellationToken ct)
    {
        var existente = await _lectorRepository.GetByDniAsync(dni.Trim(), ct);
        if (existente is not null && existente.Id != lectorIdActual)
        {
            throw new DniDuplicadoException(dni.Trim());
        }
    }

    private static LectorResponseDTO MapToResponseDTO(Lector lector) => new()
    {
        Id = lector.Id,
        Nombre = lector.Nombre,
        Apellido = lector.Apellido,
        Email = lector.Email,
        Dni = lector.Dni,
        SuspendidoHasta = lector.SuspendidoHasta
    };
}
