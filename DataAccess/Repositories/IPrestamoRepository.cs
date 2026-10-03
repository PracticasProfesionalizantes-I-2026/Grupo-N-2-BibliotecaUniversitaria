using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface IPrestamoRepository
{
    Task<IReadOnlyList<Prestamo>> GetAllAsync(CancellationToken ct = default);
    Task<Prestamo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Prestamo> CreateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task UpdateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task<bool> TieneActivosPorLibroAsync(Guid libroId, CancellationToken ct = default);
    Task<bool> TieneActivosPorLectorAsync(Guid lectorId, CancellationToken ct = default);
}
