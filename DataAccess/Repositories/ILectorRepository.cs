using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface ILectorRepository
{
    Task<IReadOnlyList<Lector>> GetAllAsync(CancellationToken ct = default);
    Task<Lector?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Lector?> GetByIdentificadorAsync(string identificador, CancellationToken ct = default);
    Task<Lector> CreateAsync(Lector lector, CancellationToken ct = default);
    Task UpdateAsync(Lector lector, CancellationToken ct = default);
    Task DeleteAsync(Lector lector, CancellationToken ct = default);
}
