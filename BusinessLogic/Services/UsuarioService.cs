using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Validation;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Usuarios;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class UsuarioService : IUsuarioService
{
    private const int EmailMaxLength = 150;

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
        var email = ValidarEmail(dto.Email);
        ValidarContraseña(dto.Password);
        await ValidarEmailUnicoAsync(email, usuarioIdActual: null, ct);
        var usuario = new Usuario
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            RolUsuario = ParseRol(dto.RolUsuario)
        };
        var creado = await _usuarioRepository.CreateAsync(usuario, ct);
        return MapToResponseDTO(creado);
    }
    public async Task<UsuarioResponseDTO> UpdateAsync(Guid id, UsuarioUpdateDTO dto, Guid currentUserId, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException("Usuario no encontrado");
        var email = ValidarEmail(dto.Email);
        await ValidarEmailUnicoAsync(email, usuarioIdActual: id, ct);

        var nuevoRol = string.IsNullOrWhiteSpace(dto.RolUsuario) ? usuario.RolUsuario : ParseRol(dto.RolUsuario);
        var seDesactiva = dto.Activo.HasValue && !dto.Activo.Value && usuario.Activo;
        var dejaDeSerAdministrador = usuario.RolUsuario == RolUsuario.Administrador && nuevoRol != RolUsuario.Administrador;

        if (id == currentUserId && seDesactiva)
        {
            throw new UsuarioAutoBajaException();
        }

        if (usuario.RolUsuario == RolUsuario.Administrador && usuario.Activo && (seDesactiva || dejaDeSerAdministrador))
        {
            var administradoresActivos = await _usuarioRepository.CountAdministradoresActivosAsync(ct);
            if (administradoresActivos <= 1)
            {
                throw new UltimoAdministradorException();
            }
        }

        usuario.Email = email;
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
    public async Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct) ?? throw new UsuarioNotFoundException("Usuario no encontrado");

        if (id == currentUserId)
        {
            throw new UsuarioAutoBajaException();
        }

        if (usuario.Activo)
        {
            if (usuario.RolUsuario == RolUsuario.Administrador)
            {
                var administradoresActivos = await _usuarioRepository.CountAdministradoresActivosAsync(ct);
                if (administradoresActivos <= 1)
                {
                    throw new UltimoAdministradorException();
                }
            }

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
    private static string ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UsuarioInvalidoException(
                "Email es obligatorio.");
        }

        var emailNormalizado = email.Trim().ToLowerInvariant();

        if (emailNormalizado.Length > EmailMaxLength)
        {
            throw new UsuarioInvalidoException($"El email no puede superar los {EmailMaxLength} caracteres.");
        }

        if (!EmailValidator.EsFormatoValido(emailNormalizado))
        {
            throw new UsuarioInvalidoException("El email no tiene un formato válido.");
        }

        return emailNormalizado;
    }
    private async Task ValidarEmailUnicoAsync(string email, Guid? usuarioIdActual, CancellationToken ct)
    {
        var existente = await _usuarioRepository.GetByEmailAsync(email, ct);
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
