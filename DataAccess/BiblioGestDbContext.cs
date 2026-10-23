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
    public DbSet<Usuario> Usuarios => Set<Usuario>();   

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Libro>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(l => l.Autor).IsRequired().HasMaxLength(150);
            entity.Property(l => l.Isbn).IsRequired().HasMaxLength(13);
            entity.Property(l => l.Ubicacion).IsRequired().HasMaxLength(50);
            entity.HasIndex(l => l.Isbn).IsUnique();
        });

        modelBuilder.Entity<Lector>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Email).IsRequired().HasMaxLength(150);
            entity.Property(l => l.Identificador).IsRequired().HasMaxLength(20);
            entity.HasIndex(l => l.Identificador).IsUnique();
            entity.HasIndex(l => l.Email).IsUnique();
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

            // Un lector no puede tener dos préstamos activos del mismo libro (RN de duplicados).
            entity.HasIndex(p => new { p.LectorId, p.LibroId })
                .IsUnique()
                .HasFilter("\"Estado\" = 'Activo'");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.RolUsuario)
                .HasConversion<string>();
            entity.Property(u => u.Activo);
        });

        base.OnModelCreating(modelBuilder);
    }
}
