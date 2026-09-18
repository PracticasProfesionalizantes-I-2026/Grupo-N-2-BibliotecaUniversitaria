using BiblioGest.DataAccess.Entities;

namespace BiblioGest.DataAccess;

public static class DbInitializer
{
    public static void Initialize(BiblioGestDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Libros.Any() || context.Lectores.Any())
        {
            return;
        }

        var libros = new List<Libro>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Titulo = "Cien Años de Soledad",
                Autor = "Gabriel García Márquez",
                Isbn = "978-0307474728",
                Ubicacion = "Estante A1",
                Stock = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titulo = "Ficciones",
                Autor = "Jorge Luis Borges",
                Isbn = "978-8433914091",
                Ubicacion = "Estante A2",
                Stock = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                Titulo = "Rayuela",
                Autor = "Julio Cortázar",
                Isbn = "978-8437604572",
                Ubicacion = "Estante A3",
                Stock = 0
            }
        };

        var lectores = new List<Lector>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Ana",
                Apellido = "Gómez",
                Email = "ana.gomez@example.com",
                Identificador = "30111222"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Bruno",
                Apellido = "Díaz",
                Email = "bruno.diaz@example.com",
                Identificador = "L-4521"
            }
        };

        context.Libros.AddRange(libros);
        context.Lectores.AddRange(lectores);
        context.SaveChanges();

        var prestamo = new Prestamo
        {
            Id = Guid.NewGuid(),
            LectorId = lectores[0].Id,
            LibroId = libros[2].Id,
            FechaPrestamo = DateTime.UtcNow.AddDays(-20),
            FechaVencimiento = DateTime.UtcNow.AddDays(-6),
            Estado = EstadoPrestamo.Activo
        };

        context.Prestamos.Add(prestamo);
        context.SaveChanges();
    }
}
