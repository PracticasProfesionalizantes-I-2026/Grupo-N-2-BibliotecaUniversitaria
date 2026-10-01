using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Usuarios;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace BiblioGest.BusinessLogic.Services;

public class UsuarioService : IUsuarioService
{
    private const int LongitudMinimaPassword = 6;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public UsuarioService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IReadOnlyList<UsuarioResponseDTO>> GetAllAsync(CancellationToken ct = default)
    {
        var usuarios = await _usuarioRepository.GetAllAsync(ct);
        return usuarios.Select(MapToResponseDTO).ToList();
    }

    public async Task<UsuarioResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException(id);
        return MapToResponseDTO(usuario);
    }

    public async Task<UsuarioResponseDTO> CreateAsync(UsuarioCreateDTO dto, CancellationToken ct = default)
    {
        ValidarDatos(dto.Nombre, dto.Email, dto.Rol);
        ValidarPassword(dto.Password);
        await ValidarEmailUnicoAsync(dto.Email, usuarioIdActual: null, ct);

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Email = dto.Email.Trim(),
            Rol = Enum.Parse<RolUsuario>(dto.Rol),
            Activo = true
        };
        usuario.PasswordHash = _passwordHasher.HashPassword(usuario, dto.Password);

        var creado = await _usuarioRepository.CreateAsync(usuario, ct);
        return MapToResponseDTO(creado);
    }

    public async Task<UsuarioResponseDTO> UpdateAsync(Guid id, UsuarioUpdateDTO dto, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException(id);

        ValidarDatos(dto.Nombre, dto.Email, dto.Rol);
        await ValidarEmailUnicoAsync(dto.Email, usuarioIdActual: id, ct);

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Email = dto.Email.Trim();
        usuario.Rol = Enum.Parse<RolUsuario>(dto.Rol);

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            ValidarPassword(dto.Password);
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, dto.Password);
        }

        await _usuarioRepository.UpdateAsync(usuario, ct);
        return MapToResponseDTO(usuario);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException(id);

        usuario.Activo = false;
        await _usuarioRepository.UpdateAsync(usuario, ct);
    }

    private static void ValidarDatos(string nombre, string email, string rol)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(rol))
        {
            throw new UsuarioInvalidoException("Nombre, email y rol son obligatorios.");
        }
    }

    private static void ValidarPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < LongitudMinimaPassword)
        {
            throw new UsuarioInvalidoException($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");
        }
    }

    private async Task ValidarEmailUnicoAsync(string email, Guid? usuarioIdActual, CancellationToken ct)
    {
        var existente = await _usuarioRepository.GetByEmailAsync(email.Trim(), ct);
        if (existente is not null && existente.Id != usuarioIdActual)
        {
            throw new EmailDuplicadoException(email);
        }
    }

    private static UsuarioResponseDTO MapToResponseDTO(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Email = usuario.Email,
        Rol = usuario.Rol.ToString(),
        Activo = usuario.Activo
    };
}
