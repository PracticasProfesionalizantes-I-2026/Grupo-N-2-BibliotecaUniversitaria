using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Auth;
using BiblioGest.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO dto, CancellationToken ct)
    {
        try
        {
            var response = await _authService.LoginAsync(dto, ct);
            return Ok(response);
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}