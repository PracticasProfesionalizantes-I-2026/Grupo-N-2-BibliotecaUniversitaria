using BiblioGest.Shared.DTOs.Reservas;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IReservaService
{
    Task<IReadOnlyList<ReservaResponseDTO>> GetColaPorLibroAsync(int libroId, CancellationToken ct = default);
    Task<ReservaResponseDTO> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ReservaResponseDTO> CreateAsync(ReservaCreateDTO dto, CancellationToken ct = default);
    Task<ReservaResponseDTO> CancelarAsync(int id, CancellationToken ct = default);
}
