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
    // guardarCambios = false difiere el SaveChanges (lo usan ColaDeReservas,
    // para procesar toda la cascada de vencimientos, y
    // PrestamoService.CerrarPrestamoAsync, para persistir toda la operación
    // de cierre, con un único SaveChanges en cada caso).
    Task UpdateAsync(Reserva reserva, CancellationToken ct = default, bool guardarCambios = true);
}
