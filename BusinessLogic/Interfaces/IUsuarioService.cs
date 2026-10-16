using BiblioGest.Shared.DTOs.Usuarios;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioResponseDTO>> GetAllAsync(CancellationToken ct = default);
    Task<UsuarioResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UsuarioResponseDTO> CreateAsync(UsuarioCreateDTO dto, CancellationToken ct = default);
    Task<UsuarioResponseDTO> UpdateAsync(Guid id, UsuarioUpdateDTO dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
