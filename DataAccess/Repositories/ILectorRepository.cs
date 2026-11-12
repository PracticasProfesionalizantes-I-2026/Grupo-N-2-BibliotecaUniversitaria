using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface ILectorRepository
{
    // pageNumber/pageSize nulos = sin paginar (se usa para reportes, que
    // necesitan el conjunto completo); el listado paginado de la API siempre
    // los manda con valores concretos (ver LectorService.GetAllAsync).
    Task<IReadOnlyList<Lector>> GetAllAsync(int? pageNumber = null, int? pageSize = null, CancellationToken ct = default);
    Task<Lector?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Lector?> GetByDniAsync(string dni, CancellationToken ct = default);
    Task<Lector?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Lector> CreateAsync(Lector lector, CancellationToken ct = default);
    Task UpdateAsync(Lector lector, CancellationToken ct = default);
    Task DeleteAsync(Lector lector, CancellationToken ct = default);
}
