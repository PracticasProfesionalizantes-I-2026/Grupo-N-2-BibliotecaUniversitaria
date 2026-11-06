using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess.Repositories;

public class ReservaRepository : IReservaRepository
{
    private readonly BiblioGestDbContext _context;

    public ReservaRepository(BiblioGestDbContext context)
    {
        _context = context;
    }

    public Task<Reserva?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Reservas
            .AsNoTracking()
            .Include(r => r.Lector)
            .Include(r => r.Libro)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Reserva>> GetActivasPorLibroAsync(int libroId, CancellationToken ct = default) =>
        await _context.Reservas
            .AsNoTracking()
            .Include(r => r.Lector)
            .Include(r => r.Libro)
            .Where(r => r.LibroId == libroId &&
                        (r.Estado == EstadoReserva.EnCola || r.Estado == EstadoReserva.Asignada))
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

    public Task<Reserva?> GetAsignadaAsync(int lectorId, int libroId, CancellationToken ct = default) =>
        _context.Reservas
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.LectorId == lectorId &&
                r.LibroId == libroId &&
                r.Estado == EstadoReserva.Asignada, ct);

    public Task<bool> ExisteActivaAsync(int lectorId, int libroId, CancellationToken ct = default) =>
        _context.Reservas
            .AsNoTracking()
            .AnyAsync(r =>
                r.LectorId == lectorId &&
                r.LibroId == libroId &&
                (r.Estado == EstadoReserva.EnCola || r.Estado == EstadoReserva.Asignada), ct);

    public Task<bool> TieneActivasPorLibroAsync(int libroId, CancellationToken ct = default) =>
        _context.Reservas
            .AsNoTracking()
            .AnyAsync(r =>
                r.LibroId == libroId &&
                (r.Estado == EstadoReserva.EnCola || r.Estado == EstadoReserva.Asignada), ct);

    public Task<bool> TieneActivasPorLectorAsync(int lectorId, CancellationToken ct = default) =>
        _context.Reservas
            .AsNoTracking()
            .AnyAsync(r =>
                r.LectorId == lectorId &&
                (r.Estado == EstadoReserva.EnCola || r.Estado == EstadoReserva.Asignada), ct);

    public async Task<Reserva> CreateAsync(Reserva reserva, CancellationToken ct = default)
    {
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync(ct);
        return reserva;
    }

    public async Task UpdateAsync(Reserva reserva, CancellationToken ct = default)
    {
        var existente = await _context.Reservas.FindAsync([reserva.Id], ct);
        if (existente is null)
        {
            return;
        }

        _context.Entry(existente).CurrentValues.SetValues(reserva);
        await _context.SaveChangesAsync(ct);
    }
}
