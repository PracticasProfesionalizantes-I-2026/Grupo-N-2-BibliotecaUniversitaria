using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("BiblioGest") ?? "Data Source=bibliogest.db";
builder.Services.AddDbContext<BiblioGestDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<ILibroRepository, LibroRepository>();
builder.Services.AddScoped<ILectorRepository, LectorRepository>();
builder.Services.AddScoped<IPrestamoRepository, PrestamoRepository>();

builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<ILectorService, LectorService>();
builder.Services.AddScoped<IPrestamoService, PrestamoService>();

var app = builder.Build();

// Crea la base de datos y carga los datos de prueba si hace falta (DbInitializer).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
    DbInitializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
