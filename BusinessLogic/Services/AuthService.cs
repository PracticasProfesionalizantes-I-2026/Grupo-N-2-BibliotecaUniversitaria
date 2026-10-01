using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BiblioGest.BusinessLogic.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthService(IUsuarioRepository usuarioRepository, IConfiguration configuration)
    {
        _usuarioRepository = usuarioRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByEmailAsync(dto.Email.Trim(), ct);

        // RN-01: usuario registrado y activo. Usa la misma excepción que una
        // contraseña incorrecta (CU-01, 3a/3b) para no revelar cuál dato falló.
        if (usuario is null || !usuario.Activo)
        {
            throw new CredencialesInvalidasException();
        }

        // RN-02: verificar el hash de la contraseña.
        var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            throw new CredencialesInvalidasException();
        }

        var (token, expiracion) = GenerarToken(usuario);

        return new LoginResponseDTO
        {
            Token = token,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol.ToString(),
            ExpiraEn = expiracion
        };
    }

    private (string Token, DateTime Expiracion) GenerarToken(Usuario usuario)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");
        var issuer = _configuration["Jwt:Issuer"] ?? "BiblioGest";
        var audience = _configuration["Jwt:Audience"] ?? "BiblioGest.Clients";
        var minutos = _configuration.GetValue("Jwt:ExpirationMinutes", 60);

        var expiracion = DateTime.UtcNow.AddMinutes(minutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiracion,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
