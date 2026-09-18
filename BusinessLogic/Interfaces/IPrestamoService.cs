using BiblioGest.Shared.DTOs.Prestamos;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IPrestamoService
{
    Task<PrestamoResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PrestamoResponseDTO> CreateAsync(PrestamoCreateDTO dto, CancellationToken ct = default);
    Task<PrestamoResponseDTO> RegistrarDevolucionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PrestamoResponseDTO>> GetVencidosAsync(CancellationToken ct = default);
}
