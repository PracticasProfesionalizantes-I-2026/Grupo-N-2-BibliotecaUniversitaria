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

    public async Task<IReadOnlyList<Lector>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Lectores.AsNoTracking().OrderBy(l => l.Apellido).ToListAsync(ct);

    public Task<Lector?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Lectores.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Lector?> GetByIdentificadorAsync(string identificador, CancellationToken ct = default) =>
        _context.Lectores.AsNoTracking().FirstOrDefaultAsync(l => l.Identificador == identificador, ct);

    public async Task<Lector> CreateAsync(Lector lector, CancellationToken ct = default)
    {
        lector.Id = Guid.NewGuid();
        _context.Lectores.Add(lector);
        await _context.SaveChangesAsync(ct);
        return lector;
    }

    public async Task UpdateAsync(Lector lector, CancellationToken ct = default)
    {
        _context.Lectores.Update(lector);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Lector lector, CancellationToken ct = default)
    {
        _context.Lectores.Remove(lector);
        await _context.SaveChangesAsync(ct);
    }
}
