using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface ILibroRepository
{
    Task<IReadOnlyList<Libro>> GetAllAsync(string? busqueda, CancellationToken ct = default);
    Task<Libro?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Libro> CreateAsync(Libro libro, CancellationToken ct = default);
    Task UpdateAsync(Libro libro, CancellationToken ct = default);
    Task DeleteAsync(Libro libro, CancellationToken ct = default);
}
