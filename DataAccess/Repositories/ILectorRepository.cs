using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface ILectorRepository
{
    Task<IReadOnlyList<Lector>> GetAllAsync(CancellationToken ct = default);
    Task<Lector?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Lector?> GetByDniAsync(string dni, CancellationToken ct = default);
    Task<Lector?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Lector> CreateAsync(Lector lector, CancellationToken ct = default);
    // guardarCambios = false difiere el SaveChanges (lo usa
    // PrestamoService.CerrarPrestamoAsync para persistir toda la operación
    // de cierre con un único SaveChanges).
    Task UpdateAsync(Lector lector, CancellationToken ct = default, bool guardarCambios = true);
    Task DeleteAsync(Lector lector, CancellationToken ct = default);
}
