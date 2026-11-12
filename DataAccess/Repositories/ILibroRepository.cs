using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface ILibroRepository
{
    Task<IReadOnlyList<Libro>> GetAllAsync(string? busqueda, CancellationToken ct = default);
    Task<Libro?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteIsbnAsync(string isbnSinGuiones, int? excluirLibroId, CancellationToken ct = default);
    Task<Libro> CreateAsync(Libro libro, CancellationToken ct = default);
    // guardarCambios = false difiere el SaveChanges (lo usa
    // PrestamoService.CerrarPrestamoAsync para persistir toda la operación
    // de cierre con un único SaveChanges).
    Task UpdateAsync(Libro libro, CancellationToken ct = default, bool guardarCambios = true);
    Task DeleteAsync(Libro libro, CancellationToken ct = default);
}
