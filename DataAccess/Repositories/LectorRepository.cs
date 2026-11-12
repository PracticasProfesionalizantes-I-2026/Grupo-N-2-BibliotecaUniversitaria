using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess.Repositories;

public class LectorRepository : ILectorRepository
{
    private readonly BiblioGestDbContext _context;

    public LectorRepository(BiblioGestDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Lector>> GetAllAsync(int? pageNumber = null, int? pageSize = null, CancellationToken ct = default)
    {
        var ordenado = _context.Lectores.AsNoTracking().OrderBy(l => l.Apellido);
        if (pageNumber is > 0 && pageSize is > 0)
        {
            return await ordenado.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync(ct);
        }

        return await ordenado.ToListAsync(ct);
    }

    public Task<Lector?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Lectores.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Lector?> GetByDniAsync(string dni, CancellationToken ct = default) =>
        _context.Lectores.AsNoTracking().FirstOrDefaultAsync(l => l.Dni == dni, ct);

    public Task<Lector?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _context.Lectores.AsNoTracking().FirstOrDefaultAsync(l => l.Email == email, ct);

    public async Task<Lector> CreateAsync(Lector lector, CancellationToken ct = default)
    {
        _context.Lectores.Add(lector);
        await _context.SaveChangesAsync(ct);
        return lector;
    }

    public async Task UpdateAsync(Lector lector, CancellationToken ct = default, bool guardarCambios = true)
    {
        _context.Lectores.Update(lector);

        if (guardarCambios)
        {
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(Lector lector, CancellationToken ct = default)
    {
        _context.Lectores.Remove(lector);
        await _context.SaveChangesAsync(ct);
    }
}
