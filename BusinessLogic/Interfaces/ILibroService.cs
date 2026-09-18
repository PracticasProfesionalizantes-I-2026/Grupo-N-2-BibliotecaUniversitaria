using BiblioGest.Shared.DTOs.Libros;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface ILibroService
{
    Task<IReadOnlyList<LibroResponseDTO>> GetAllAsync(string? busqueda, CancellationToken ct = default);
    Task<LibroResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LibroResponseDTO> CreateAsync(LibroCreateDTO dto, CancellationToken ct = default);
    Task<LibroResponseDTO> UpdateAsync(Guid id, LibroUpdateDTO dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
