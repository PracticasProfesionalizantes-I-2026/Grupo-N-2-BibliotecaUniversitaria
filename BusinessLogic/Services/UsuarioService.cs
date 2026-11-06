using System.Text.RegularExpressions;
using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Usuarios;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class UsuarioService : IUsuarioService
{
    private const int LargoMaximoContraseña = 64;
    private const int LargoMinimoEmail = 6;
    private const int LargoMaximoEmail = 100;
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    public UsuarioService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }
    public async Task<IReadOnlyList<UsuarioResponseDTO>> GetAllAsync(CancellationToken ct = default)
    {
        var usuarios = await _usuarioRepository.GetAllAsync(ct);
        return usuarios.Select(MapToResponseDTO).ToList();
    }
    public async Task<UsuarioResponseDTO> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException("Usuario no encontrado");
        return MapToResponseDTO(usuario);

    }
    public async Task<UsuarioResponseDTO> CreateAsync(UsuarioCreateDTO dto, CancellationToken ct = default)
    {
        ValidarEmail(dto.Email);
        ValidarContraseña(dto.Password);
        await ValidarEmailUnicoAsync(dto.Email, usuarioIdActual: null, ct);
        var usuario = new Usuario
        {
            Email = dto.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(dto.Password),
            RolUsuario = ParseRol(dto.RolUsuario)
        };
        var creado = await _usuarioRepository.CreateAsync(usuario, ct);
        return MapToResponseDTO(creado);
    }
    public async Task<UsuarioResponseDTO> UpdateAsync(int id, UsuarioUpdateDTO dto, int usuarioActualId, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException("Usuario no encontrado");
        var email = string.IsNullOrWhiteSpace(dto.Email) ? usuario.Email : dto.Email;
        ValidarEmail(email);
        await ValidarEmailUnicoAsync(email, usuarioIdActual: id, ct);

        var nuevoRol = string.IsNullOrWhiteSpace(dto.RolUsuario) ? usuario.RolUsuario : ParseRol(dto.RolUsuario);
        var seDesactiva = dto.Activo == false && usuario.Activo;
        var dejaDeSerAdministrador = usuario.RolUsuario == RolUsuario.Administrador && nuevoRol != RolUsuario.Administrador;

        if (id == usuarioActualId && (dto.Activo == false || dejaDeSerAdministrador))
        {
            throw new UsuarioAutoModificacionException();
        }

        if (usuario.RolUsuario == RolUsuario.Administrador && usuario.Activo && (seDesactiva || dejaDeSerAdministrador))
        {
            await ValidarQueNoEsElUltimoAdministradorAsync(ct);
        }

        usuario.Email = email.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            ValidarContraseña(dto.Password);
            usuario.PasswordHash = _passwordHasher.Hash(dto.Password);
        }
        usuario.RolUsuario = nuevoRol;
        if (dto.Activo.HasValue)
        {
            usuario.Activo = dto.Activo.Value;
        }
        await _usuarioRepository.UpdateAsync(usuario, ct);
        return MapToResponseDTO(usuario);
    }
    public async Task DeleteAsync(int id, int usuarioActualId, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException("Usuario no encontrado");

        if (id == usuarioActualId)
        {
            throw new UsuarioAutoBajaException();
        }

        if (usuario.Activo)
        {
            if (usuario.RolUsuario == RolUsuario.Administrador)
            {
                await ValidarQueNoEsElUltimoAdministradorAsync(ct);
            }

            usuario.Activo = false;
            await _usuarioRepository.UpdateAsync(usuario, ct);
        }
    }
    private async Task ValidarQueNoEsElUltimoAdministradorAsync(CancellationToken ct)
    {
        var administradoresActivos = await _usuarioRepository.CountAdministradoresActivosAsync(ct);
        if (administradoresActivos <= 1)
        {
            throw new UltimoAdministradorException();
        }
    }
    private static void ValidarContraseña(string password)
    {
        if (string.IsNullOrWhiteSpace(password)|| password.Length < 6)
        {
            throw new UsuarioInvalidoException(
                "Contraseña es obligatoria y debe tener al menos 6 caracteres.");
        }

        if (password.Length > LargoMaximoContraseña)
        {
            throw new UsuarioInvalidoException(
                $"La contraseña no puede superar los {LargoMaximoContraseña} caracteres.");
        }
    }
    private static void ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UsuarioInvalidoException(
                "Email es obligatorio.");
        }

        var largo = email.Trim().Length;
        if (largo < LargoMinimoEmail || largo > LargoMaximoEmail)
        {
            throw new UsuarioInvalidoException(
                $"El email debe tener entre {LargoMinimoEmail} y {LargoMaximoEmail} caracteres.");
        }

        if (!EmailRegex.IsMatch(email.Trim()))
        {
            throw new UsuarioInvalidoException(
                "El email no tiene un formato válido (ejemplo: nombre@dominio.com).");
        }
    }
    private async Task ValidarEmailUnicoAsync(string email, int? usuarioIdActual, CancellationToken ct)
    {
        var existente = await _usuarioRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), ct);
        if (existente is not null && existente.Id != usuarioIdActual)
        {
            throw new UsuarioDuplicadoException("El email ya existe.");
        }
    }
    private static UsuarioResponseDTO MapToResponseDTO(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Email = usuario.Email,
        RolUsuario = usuario.RolUsuario.ToString(),
        Activo = usuario.Activo
    };
    private static RolUsuario ParseRol(string rol)
    {
        if (Enum.TryParse<RolUsuario>(rol, true, out var rolUsuario) && Enum.IsDefined(rolUsuario))
        {
            return rolUsuario;
        }
        throw new UsuarioInvalidoException("Rol del usuario inválido.");
    }
}




