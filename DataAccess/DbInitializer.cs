using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess;

public static class DbInitializer
{
    public static void Initialize(BiblioGestDbContext context, Func<string, string> hashPassword)
    {
        context.Database.Migrate();

        if (!context.Usuarios.Any())
        {
            var usuarios = new List<Usuario>
            {
                new()
                {
                    Email = "admin@bibliogest.com",
                    PasswordHash = hashPassword("Admin123!"),
                    RolUsuario = RolUsuario.Administrador,
                    Activo = true
                },
                new()
                {
                    Email = "bibliotecario@bibliogest.com",
                    PasswordHash = hashPassword("Bibliotecario123!"),
                    RolUsuario = RolUsuario.Bibliotecario,
                    Activo = true
                }
            };

            context.Usuarios.AddRange(usuarios);
            context.SaveChanges();
        }

        if (context.Libros.Any() || context.Lectores.Any())
        {
            return;
        }

        var libros = new List<Libro>
        {
            new()
            {
                Titulo = "Cien Años de Soledad",
                Autor = "Gabriel García Márquez",
                Isbn = "978-0307474728",
                Ubicacion = "A1-1",
                Stock = 3
            },
            new()
            {
                Titulo = "Ficciones",
                Autor = "Jorge Luis Borges",
                Isbn = "978-8433914091",
                Ubicacion = "A1-2",
                Stock = 2
            },
            new()
            {
                Titulo = "Rayuela",
                Autor = "Julio Cortázar",
                Isbn = "978-8437604572",
                Ubicacion = "A2-1",
                Stock = 0
            }
        };

        var lectores = new List<Lector>
        {
            new()
            {
                Nombre = "Ana",
                Apellido = "Gómez",
                Email = "ana.gomez@example.com",
                Dni = "30111222"
            },
            new()
            {
                Nombre = "Bruno",
                Apellido = "Díaz",
                Email = "bruno.diaz@example.com",
                Dni = "28456789"
            }
        };

        context.Libros.AddRange(libros);
        context.Lectores.AddRange(lectores);
        context.SaveChanges();

        var prestamo = new Prestamo
        {
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
