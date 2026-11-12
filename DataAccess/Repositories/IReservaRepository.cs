using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess.Repositories;

public interface IReservaRepository
{
    Task<Reserva?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Reserva>> GetActivasPorLibroAsync(int libroId, CancellationToken ct = default);
    Task<Reserva?> GetAsignadaAsync(int lectorId, int libroId, CancellationToken ct = default);
    Task<bool> ExisteActivaAsync(int lectorId, int libroId, CancellationToken ct = default);
    Task<bool> TieneActivasPorLibroAsync(int libroId, CancellationToken ct = default);
    Task<bool> TieneActivasPorLectorAsync(int lectorId, CancellationToken ct = default);
    Task<Reserva> CreateAsync(Reserva reserva, CancellationToken ct = default);
    Task UpdateAsync(Reserva reserva, CancellationToken ct = default, bool guardarCambios = true);
}
