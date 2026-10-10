using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Usuarios;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class UsuarioService : IUsuarioService
{
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
    public async Task<UsuarioResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default)
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
    public async Task<UsuarioResponseDTO> UpdateAsync(Guid id, UsuarioUpdateDTO dto, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException("Usuario no encontrado");
        ValidarEmail(dto.Email);
        await ValidarEmailUnicoAsync(dto.Email, usuarioIdActual: id, ct);
        usuario.Email = dto.Email.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            ValidarContraseña(dto.Password);
            usuario.PasswordHash = _passwordHasher.Hash(dto.Password);
        }
        if (!string.IsNullOrWhiteSpace(dto.RolUsuario))
        {
            usuario.RolUsuario = ParseRol(dto.RolUsuario);
        }
        if (dto.Activo.HasValue)
        {
            usuario.Activo = dto.Activo.Value;
        }
        await _usuarioRepository.UpdateAsync(usuario, ct);
        return MapToResponseDTO(usuario);
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException("Usuario no encontrado");
        if (usuario.Activo)
        {
            usuario.Activo = false;
            await _usuarioRepository.UpdateAsync(usuario, ct);
        }
    }
    private static void ValidarContraseña(string password)
    {
        if (string.IsNullOrWhiteSpace(password)|| password.Length < 6)
        {
            throw new UsuarioInvalidoException(
                "Contraseña es obligatoria y debe tener al menos 6 caracteres.");
        }
    }
    private static void ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UsuarioInvalidoException(
                "Email es obligatorio.");
        }
    }
    private async Task ValidarEmailUnicoAsync(string email, Guid? usuarioIdActual, CancellationToken ct)
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




