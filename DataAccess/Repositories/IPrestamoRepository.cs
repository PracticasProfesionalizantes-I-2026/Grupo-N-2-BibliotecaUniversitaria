using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface IPrestamoRepository
{
    Task<Prestamo?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Prestamo> CreateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task UpdateAsync(Prestamo prestamo, CancellationToken ct = default);
    Task<int> CountActivosPorLectorAsync(int lectorId, CancellationToken ct = default);
    Task<bool> TieneActivosPorLibroAsync(int libroId, CancellationToken ct = default);
    Task<bool> TieneActivosPorLectorAsync(int lectorId, CancellationToken ct = default);
    Task<bool> TienePrestamosPorLectorAsync(int lectorId, CancellationToken ct = default);
    Task<bool> TienePrestamosPorLibroAsync(int libroId, CancellationToken ct = default);
    Task<bool> LectorTieneMoraAsync(int lectorId, CancellationToken ct = default);
    Task<bool> LectorTieneExtraviadosAsync(int lectorId, CancellationToken ct = default);
    Task<IReadOnlyList<Prestamo>> GetVencidosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Prestamo>> GetPorRangoAsync(DateTime? desde, DateTime? hastaExclusivo, CancellationToken ct = default);
}
