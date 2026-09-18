using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess;

public class BiblioGestDbContext : DbContext
{
    public BiblioGestDbContext(DbContextOptions<BiblioGestDbContext> options) : base(options)
    {
    }

    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Lector> Lectores => Set<Lector>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Libro>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Titulo).IsRequired();
            entity.Property(l => l.Autor).IsRequired();
            entity.Property(l => l.Isbn).IsRequired();
            entity.Property(l => l.Ubicacion).IsRequired();
        });

        modelBuilder.Entity<Lector>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Nombre).IsRequired();
            entity.Property(l => l.Apellido).IsRequired();
            entity.Property(l => l.Email).IsRequired();
            entity.Property(l => l.Identificador).IsRequired();
            entity.HasIndex(l => l.Identificador).IsUnique();
        });

        modelBuilder.Entity<Prestamo>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.HasOne(p => p.Libro)
                .WithMany(l => l.Prestamos)
                .HasForeignKey(p => p.LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Lector)
                .WithMany(l => l.Prestamos)
                .HasForeignKey(p => p.LectorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.Estado)
                .HasConversion<string>();
        });

        base.OnModelCreating(modelBuilder);
    }
}
