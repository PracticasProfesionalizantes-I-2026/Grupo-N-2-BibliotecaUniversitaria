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

    public Task<Prestamo?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Prestamo> CreateAsync(Prestamo prestamo, CancellationToken ct = default)
    {
        _context.Prestamos.Add(prestamo);
        await _context.SaveChangesAsync(ct);
        return prestamo;
    }

    public async Task UpdateAsync(Prestamo prestamo, CancellationToken ct = default)
    {
        _context.Prestamos.Update(prestamo);
        await _context.SaveChangesAsync(ct);
    }

    public Task<int> CountActivosPorLectorAsync(int lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .CountAsync(p => p.LectorId == lectorId && p.Estado == EstadoPrestamo.Activo, ct);

    public Task<bool> TieneActivosPorLibroAsync(int libroId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LibroId == libroId && (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Extraviado), ct);

    public Task<bool> TieneActivosPorLectorAsync(int lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LectorId == lectorId && (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Extraviado), ct);

    public Task<bool> TienePrestamosPorLectorAsync(int lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LectorId == lectorId, ct);

    public Task<bool> TienePrestamosPorLibroAsync(int libroId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LibroId == libroId, ct);

    public Task<bool> LectorTieneMoraAsync(int lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p =>
                p.LectorId == lectorId &&
                p.Estado == EstadoPrestamo.Activo &&
                p.FechaVencimiento < DateTime.UtcNow, ct);

    public Task<bool> LectorTieneExtraviadosAsync(int lectorId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LectorId == lectorId && p.Estado == EstadoPrestamo.Extraviado, ct);

    public Task<bool> ExistePrestamoActivoAsync(int lectorId, int libroId, CancellationToken ct = default) =>
        _context.Prestamos
            .AsNoTracking()
            .AnyAsync(p => p.LectorId == lectorId && p.LibroId == libroId && p.Estado == EstadoPrestamo.Activo, ct);

    public async Task<IReadOnlyList<Prestamo>> GetActivosAsync(int? pageNumber = null, int? pageSize = null, CancellationToken ct = default)
    {
        var ordenado = _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .Where(p => p.Estado == EstadoPrestamo.Activo)
            .OrderBy(p => p.FechaVencimiento);

        if (pageNumber is > 0 && pageSize is > 0)
        {
            return await ordenado.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync(ct);
        }

        return await ordenado.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Prestamo>> GetVencidosAsync(CancellationToken ct = default) =>
        await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .Where(p => p.Estado == EstadoPrestamo.Activo && p.FechaVencimiento < DateTime.UtcNow)
            .OrderBy(p => p.FechaVencimiento)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Prestamo>> GetPorRangoAsync(DateTime? desde, DateTime? hastaExclusivo, CancellationToken ct = default)
    {
        var query = _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .AsQueryable();

        if (desde.HasValue)
        {
            query = query.Where(p => p.FechaPrestamo >= desde.Value);
        }

        if (hastaExclusivo.HasValue)
        {
            query = query.Where(p => p.FechaPrestamo < hastaExclusivo.Value);
        }

        return await query.OrderBy(p => p.FechaPrestamo).ToListAsync(ct);
    }
}
