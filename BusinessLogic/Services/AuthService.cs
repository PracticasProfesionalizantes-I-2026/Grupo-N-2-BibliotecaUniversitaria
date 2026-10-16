using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BiblioGest.BusinessLogic.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByEmailAsync(dto.Email, ct);

        if (usuario == null || !_passwordHasher.Verify(dto.Password, usuario.PasswordHash) || !usuario.Activo)
        {
            throw new CredencialesInvalidasException();
        }

        var token = GenerateJwtToken(usuario);

        return new LoginResponseDTO
        {
            Token = token,
            Email = usuario.Email,
            Rol = usuario.RolUsuario.ToString()
        };
    }

    private string GenerateJwtToken(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = jwtSettings["Key"]!;
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.RolUsuario.ToString())
        };

        var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}