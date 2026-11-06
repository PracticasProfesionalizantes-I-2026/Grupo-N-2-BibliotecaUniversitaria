using BiblioGest.Shared.DTOs.Usuarios;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioResponseDTO>> GetAllAsync(CancellationToken ct = default);
    Task<UsuarioResponseDTO> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UsuarioResponseDTO> CreateAsync(UsuarioCreateDTO dto, CancellationToken ct = default);
    Task<UsuarioResponseDTO> UpdateAsync(int id, UsuarioUpdateDTO dto, int usuarioActualId, CancellationToken ct = default);
    Task DeleteAsync(int id, int usuarioActualId, CancellationToken ct = default);
}
