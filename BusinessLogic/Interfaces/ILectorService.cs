using BiblioGest.Shared.DTOs.Lectores;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface ILectorService
{
    Task<IReadOnlyList<LectorResponseDTO>> GetAllAsync(int? pageNumber = null, int? pageSize = null, CancellationToken ct = default);
    Task<LectorResponseDTO> GetByIdAsync(int id, CancellationToken ct = default);
    Task<LectorResponseDTO> CreateAsync(LectorCreateDTO dto, CancellationToken ct = default);
    Task<LectorResponseDTO> UpdateAsync(int id, LectorUpdateDTO dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
