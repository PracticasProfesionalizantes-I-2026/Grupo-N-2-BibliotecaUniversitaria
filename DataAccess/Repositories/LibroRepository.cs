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

    public async Task<IReadOnlyList<Libro>> GetAllAsync(string? busqueda, int? pageNumber = null, int? pageSize = null, CancellationToken ct = default)
    {
        var query = _context.Libros.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(l =>
                EF.Functions.Like(l.Titulo, $"%{busqueda}%") ||
                EF.Functions.Like(l.Autor, $"%{busqueda}%") ||
                EF.Functions.Like(l.Isbn, $"%{busqueda}%"));
        }

        var ordenado = query.OrderBy(l => l.Titulo);
        if (pageNumber is > 0 && pageSize is > 0)
        {
            return await ordenado.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync(ct);
        }

        return await ordenado.ToListAsync(ct);
    }

    public Task<Libro?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Libros.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<bool> ExisteIsbnAsync(string isbnSinGuiones, int? excluirLibroId, CancellationToken ct = default) =>
        _context.Libros.AsNoTracking().AnyAsync(l =>
            l.Isbn.Replace("-", "").ToUpper() == isbnSinGuiones &&
            (excluirLibroId == null || l.Id != excluirLibroId), ct);

    public async Task<Libro> CreateAsync(Libro libro, CancellationToken ct = default)
    {
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
