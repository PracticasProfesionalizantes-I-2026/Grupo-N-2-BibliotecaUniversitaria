using BiblioGest.Shared.DTOs.Prestamos;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IPrestamoService
{
    Task<PrestamoResponseDTO> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PrestamoResponseDTO> CreateAsync(PrestamoCreateDTO dto, CancellationToken ct = default);
    Task<PrestamoResponseDTO> RegistrarDevolucionAsync(int id, CancellationToken ct = default);
    Task<PrestamoResponseDTO> RegistrarExtravioAsync(int id, CancellationToken ct = default);
    Task<PrestamoResponseDTO> RegistrarReposicionAsync(int id, CancellationToken ct = default);
    Task<PrestamoResponseDTO> RenovarAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PrestamoResponseDTO>> GetVencidosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PrestamoResponseDTO>> GetActivosAsync(CancellationToken ct = default);
    Task<EstadoLectorPrestamoDTO> GetEstadoLectorAsync(int lectorId, CancellationToken ct = default);
}
