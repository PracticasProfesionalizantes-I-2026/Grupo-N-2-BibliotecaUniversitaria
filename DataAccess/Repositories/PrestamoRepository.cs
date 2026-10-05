using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess.Repositories;

public class PrestamoRepository : IPrestamoRepository
{
    private readonly BiblioGestDbContext _context;

    public PrestamoRepository(BiblioGestDbContext context)
    {
        _context = context;
    }

    public Task<Prestamo?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Prestamo> CreateAsync(Prestamo prestamo, CancellationToken ct = default)
    {
        prestamo.Id = Guid.NewGuid();
        _context.Prestamos.Add(prestamo);
        await _context.SaveChangesAsync(ct);
        return prestamo;
    }

    public async Task UpdateAsync(Prestamo prestamo, CancellationToken ct = default)
    {
        _context.Prestamos.Update(prestamo);
        await _context.SaveChangesAsync(ct);
    }

    public Task<int> CountActivosPorLectorAsync(Guid lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .CountAsync(p => p.LectorId == lectorId && p.Estado == EstadoPrestamo.Activo, ct);

    public Task<bool> TieneActivosPorLibroAsync(Guid libroId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LibroId == libroId && p.Estado == EstadoPrestamo.Activo, ct);

    public Task<bool> TieneActivosPorLectorAsync(Guid lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LectorId == lectorId && p.Estado == EstadoPrestamo.Activo, ct);

    public Task<bool> LectorTieneMoraAsync(Guid lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p =>
                p.LectorId == lectorId &&
                p.Estado == EstadoPrestamo.Activo &&
                p.FechaVencimiento < DateTime.UtcNow, ct);

    public async Task<IReadOnlyList<Prestamo>> GetVencidosAsync(CancellationToken ct = default) =>
        await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .Where(p => p.Estado == EstadoPrestamo.Activo && p.FechaVencimiento < DateTime.UtcNow)
            .OrderBy(p => p.FechaVencimiento)
            .ToListAsync(ct);
}
