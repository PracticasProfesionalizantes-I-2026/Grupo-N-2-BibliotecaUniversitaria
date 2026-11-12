using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface ILibroRepository
{
    Task<IReadOnlyList<Libro>> GetAllAsync(string? busqueda, CancellationToken ct = default);
    Task<Libro?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteIsbnAsync(string isbnSinGuiones, int? excluirLibroId, CancellationToken ct = default);
    Task<Libro> CreateAsync(Libro libro, CancellationToken ct = default);
    // guardarCambios = false difiere el SaveChanges (lo usan ColaDeReservas,
    // para procesar toda la cascada de vencimientos, y
    // PrestamoService.CerrarPrestamoAsync, para persistir toda la operación
    // de cierre, con un único SaveChanges en cada caso).
    Task UpdateAsync(Libro libro, CancellationToken ct = default, bool guardarCambios = true);
    Task DeleteAsync(Libro libro, CancellationToken ct = default);
}
