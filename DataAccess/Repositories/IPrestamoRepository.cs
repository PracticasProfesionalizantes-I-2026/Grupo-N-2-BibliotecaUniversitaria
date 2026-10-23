using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface IPrestamoRepository
{
    Task<Prestamo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Prestamo> CreateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task UpdateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task<int> CountActivosPorLectorAsync(Guid lectorId, CancellationToken ct = default);
    Task<bool> TienePrestamosPorLibroAsync(Guid libroId, CancellationToken ct = default);
    Task<bool> TienePrestamosPorLectorAsync(Guid lectorId, CancellationToken ct = default);
    Task<bool> ExistePrestamoActivoAsync(Guid lectorId, Guid libroId, CancellationToken ct = default);
    Task<bool> LectorTieneMoraAsync(Guid lectorId, DateTime ahoraUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Prestamo>> GetVencidosAsync(DateTime ahoraUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Prestamo>> GetPorRangoAsync(DateTime? desde, DateTime? hastaExclusivo, CancellationToken ct = default);
}
