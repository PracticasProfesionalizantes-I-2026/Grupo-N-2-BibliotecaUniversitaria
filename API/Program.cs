using BiblioGest.Api.Middleware;
using BiblioGest.Api.OpenApi;
using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Services;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Por defecto, un error de model binding (campo null, body vacío, JSON
        // inválido) devuelve un ProblemDetails en inglés que no pasa por
        // ExceptionMiddleware. Lo reemplazamos para que toda la API responda
        // siempre el mismo formato { message } en español.
        options.InvalidModelStateResponseFactory = context =>
        {
            var entradas = context.ModelState
                .Where(kvp => kvp.Value is { Errors.Count: > 0 })
                .ToList();

            var errorDeCuerpo = entradas.FirstOrDefault(kvp =>
                kvp.Key.Length == 0 || kvp.Key == "$" || kvp.Key.StartsWith("$.", StringComparison.Ordinal));

            string mensaje;
            if (errorDeCuerpo.Value is not null)
            {
                var original = errorDeCuerpo.Value.Errors[0].ErrorMessage;
                mensaje = original.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase)
                    ? "El cuerpo de la solicitud es obligatorio y no puede estar vacío."
                    : "El cuerpo de la solicitud no es un JSON válido.";
            }
            else
            {
                var camposInvalidos = entradas
                    .Where(kvp => kvp.Key.Length > 0 && kvp.Key != "dto")
                    .Select(kvp => DescribirErrorDeCampo(kvp.Key, kvp.Value!.Errors[0].ErrorMessage))
                    .ToList();

                mensaje = camposInvalidos.Count > 0
                    ? string.Join(" ", camposInvalidos)
                    : "Hay errores de validación en los datos enviados.";
            }

            return new BadRequestObjectResult(new { message = mensaje });
        };
    });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var connectionString = builder.Configuration.GetConnectionString("BiblioGest") ?? "Data Source=bibliogest.db";
builder.Services.AddDbContext<BiblioGestDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ILibroRepository, LibroRepository>();
builder.Services.AddScoped<ILectorRepository, LectorRepository>();
builder.Services.AddScoped<IPrestamoRepository, PrestamoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<ILectorService, LectorService>();
builder.Services.AddScoped<IPrestamoService, PrestamoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Sin esto, un 401/403 resuelto por el propio middleware de JWT (sin
    // token, token invalido, vencido, o rol insuficiente) devuelve el body
    // vacio por defecto de ASP.NET, sin pasar por ExceptionMiddleware.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Evita que ASP.NET tambien escriba su respuesta 401 por defecto.
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var mensaje = context.AuthenticateFailure is null
                ? "Debe iniciar sesión."
                : "Token inválido o vencido.";
            await context.Response.WriteAsJsonAsync(new { message = mensaje });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "No tiene permisos para realizar esta acción." });
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Crea la base de datos y carga los datos de prueba si hace falta (DbInitializer).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BiblioGestDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    DbInitializer.Initialize(context, passwordHasher.Hash);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BiblioGest API v1");
        options.EnablePersistAuthorization();
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string DescribirErrorDeCampo(string campo, string mensajeOriginal)
{
    if (mensajeOriginal.Contains("is required", StringComparison.OrdinalIgnoreCase))
    {
        return $"El campo {campo} es obligatorio.";
    }

    if (mensajeOriginal.Contains("is not a valid e-mail", StringComparison.OrdinalIgnoreCase))
    {
        return $"El campo {campo} no tiene un formato de email válido.";
    }

    return $"El campo {campo} tiene un valor inválido.";
}

public partial class Program
{
}