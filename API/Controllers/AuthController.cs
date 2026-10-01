using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/v1/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO dto, CancellationToken ct)
    {
        var resultado = await _authService.LoginAsync(dto, ct);
        return Ok(resultado);
    }
}
