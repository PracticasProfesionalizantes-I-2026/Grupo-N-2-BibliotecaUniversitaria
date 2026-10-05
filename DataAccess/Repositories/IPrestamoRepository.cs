using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface IPrestamoRepository
{
    Task<Prestamo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Prestamo> CreateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task UpdateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task<int> CountActivosPorLectorAsync(Guid lectorId, CancellationToken ct = default);
    Task<bool> TieneActivosPorLibroAsync(Guid libroId, CancellationToken ct = default);
    Task<bool> TieneActivosPorLectorAsync(Guid lectorId, CancellationToken ct = default);
    Task<bool> LectorTieneMoraAsync(Guid lectorId, CancellationToken ct = default);
    Task<IReadOnlyList<Prestamo>> GetVencidosAsync(CancellationToken ct = default);
}
