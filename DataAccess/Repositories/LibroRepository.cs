using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess.Repositories;

public class LibroRepository : ILibroRepository
{
    private readonly BiblioGestDbContext _context;

    public LibroRepository(BiblioGestDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Libro>> GetAllAsync(string? busqueda, CancellationToken ct = default)
    {
        var query = _context.Libros.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(l =>
                EF.Functions.Like(l.Titulo, $"%{busqueda}%") ||
                EF.Functions.Like(l.Autor, $"%{busqueda}%") ||
                EF.Functions.Like(l.Isbn, $"%{busqueda}%"));
        }

        return await query.OrderBy(l => l.Titulo).ToListAsync(ct);
    }

    public Task<Libro?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Libros.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<Libro> CreateAsync(Libro libro, CancellationToken ct = default)
    {
        libro.Id = Guid.NewGuid();
        _context.Libros.Add(libro);
        await _context.SaveChangesAsync(ct);
        return libro;
    }

    public async Task UpdateAsync(Libro libro, CancellationToken ct = default)
    {
        _context.Libros.Update(libro);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Libro libro, CancellationToken ct = default)
    {
        _context.Libros.Remove(libro);
        await _context.SaveChangesAsync(ct);
    }
}
