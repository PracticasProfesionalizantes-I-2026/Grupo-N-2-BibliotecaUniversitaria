using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.Shared.DTOs.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioGest.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    // GET /api/v1/usuarios
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var usuarios = await _usuarioService.GetAllAsync(ct);
        return Ok(usuarios);
    }

    // GET /api/v1/usuarios/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var usuario = await _usuarioService.GetByIdAsync(id, ct);
        return Ok(usuario);
    }

    // POST /api/v1/usuarios
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateDTO dto, CancellationToken ct)
    {
        var creado = await _usuarioService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    // PUT /api/v1/usuarios/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UsuarioUpdateDTO dto, CancellationToken ct)
    {
        var actualizado = await _usuarioService.UpdateAsync(id, dto, ct);
        return Ok(actualizado);
    }

    // DELETE /api/v1/usuarios/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _usuarioService.DeleteAsync(id, ct);
        return NoContent();
    }
}
