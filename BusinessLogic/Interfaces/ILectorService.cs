using BiblioGest.Shared.DTOs.Lectores;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface ILectorService
{
    Task<IReadOnlyList<LectorResponseDTO>> GetAllAsync(CancellationToken ct = default);
    Task<LectorResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LectorResponseDTO> CreateAsync(LectorCreateDTO dto, CancellationToken ct = default);
    Task<LectorResponseDTO> UpdateAsync(Guid id, LectorUpdateDTO dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
