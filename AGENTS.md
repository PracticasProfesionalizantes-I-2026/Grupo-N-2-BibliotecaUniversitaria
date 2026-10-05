# AGENTS.md — BiblioGest API

Contexto operativo para agentes de IA (Claude Code, Copilot, etc.) que
trabajen en este repositorio.

## Qué es este proyecto

API RESTful en .NET 10 con arquitectura N-Tier (`Controller → Service →
Repository → DbContext`) para BiblioGest, un sistema de gestión de
biblioteca universitaria (Práctica Profesionalizante I, Grupo N°2).

**Alcance actual (importante):** esta API implementa únicamente los 3
casos de uso principales — Gestionar Libros, Gestionar Lectores y
Gestionar Préstamos (incluye el control de mora, que es una regla de
negocio de Préstamos, no una entidad aparte). Login/Autenticación,
Gestionar Usuarios del Sistema y Generar Reportes están **fuera de
alcance a propósito** (los implementa el equipo a mano, por indicación de
la cátedra) — no agregues código para esas funcionalidades salvo que se
pida explícitamente.

Los casos de uso completos y las reglas de negocio (RN-01 a RN-21) están
documentados en `Documentos/Casos de Uso/`.

## Comandos

```bash
dotnet build BiblioGest.slnx      # compilar toda la solución
dotnet test BiblioGest.slnx       # correr unitarios + integración
dotnet run --project API          # levantar la API (Scalar en /scalar/v1 en Development)
```

Para migraciones de EF Core (ejecutar desde `DataAccess/`, requiere el
tool `dotnet-ef` instalado globalmente):

```bash
dotnet ef migrations add <Nombre> --output-dir Migrations
dotnet ef database update
```

## Convención de capas (no romper)

- `Controller` nunca accede a datos directamente ni contiene lógica de
  negocio. Solo recibe el request, llama al service correspondiente y
  mapea excepciones a códigos HTTP con `try/catch` explícito.
- `Service` (en `BusinessLogic/`) contiene toda la validación y lógica de
  negocio. Nunca escribe LINQ-to-entities ni usa el `DbContext`
  directamente — solo llama a los repositorios (`DataAccess/Repositories`)
  que recibe por constructor (inyección de dependencias, interfaces).
  Mapea entidades a DTOs con un método privado `MapToResponseDTO`.
- `Repository` (en `DataAccess/`) es el único lugar que habla con el
  `DbContext`. Usa `AsNoTracking()` en toda lectura, y asigna el `Guid` del
  Id dentro de `CreateAsync` (nunca lo recibe del caller).
- DTOs (`Shared/DTOs/`) son manuales, sin AutoMapper, separados en
  `CreateDTO`/`UpdateDTO`/`ResponseDTO` por entidad. Nunca se expone una
  entidad de EF directamente en una respuesta HTTP.
- Excepciones (`Shared/Exceptions/`) son tipadas, una clase por archivo,
  heredando de `NotFoundException`, `ValidationException` o
  `ConflictException` (409: duplicados, límites de negocio o intentos de
  borrar con dependencias activas).

## Reglas de negocio implementadas

| Regla | Dónde se valida |
| --- | --- |
| Libro: título/autor/ISBN/ubicación obligatorios | `LibroService.CreateAsync/UpdateAsync` |
| Libro: stock no negativo | `LibroService.CreateAsync/UpdateAsync` |
| Libro: no se elimina con préstamos activos | `LibroService.DeleteAsync` |
| Lector: nombre/apellido/email/identificador obligatorios | `LectorService.CreateAsync/UpdateAsync` |
| Lector: identificador (DNI/legajo) único | `LectorService.CreateAsync/UpdateAsync` |
| Lector: no se elimina con préstamos activos | `LectorService.DeleteAsync` |
| Préstamo: máximo 3 activos por lector | `PrestamoService.CreateAsync` |
| Préstamo: no se presta un libro sin stock | `PrestamoService.CreateAsync` |
| Préstamo: lector en mora no puede pedir otro | `PrestamoService.CreateAsync` |
| Préstamo: vencimiento a 14 días | `PrestamoService.CreateAsync` |
| Préstamo: "vencido" se calcula en la consulta, no se persiste | `PrestamoRepository.GetVencidosAsync` / `LectorTieneMoraAsync` |

## Testing

- Unitarios (`tests/BiblioGest.UnitTests`): xUnit + Moq, mockeando las
  interfaces de `DataAccess/Repositories`. No deben tocar una base real.
- Integración (`tests/BiblioGest.IntegrationTests`): `WebApplicationFactory`
  con `CustomWebApplicationFactory`, que reemplaza el `DbContext` por una
  SQLite en memoria aislada por instancia — no usar ni depender de
  `bibliogest.db`.
- Antes de dar por terminado un cambio, correr `dotnet build` y
  `dotnet test` sobre `BiblioGest.slnx` y confirmar que todo sigue en
  verde.

## Otras convenciones

- No agregar autenticación/`[Authorize]`/JWT a menos que se pida
  explícitamente (ver "Alcance actual").
- `bruno/` debe tener un request por endpoint nuevo que se agregue,
  cubriendo al menos un caso de éxito y los de error relevantes.
- No commitear `bin/`, `obj/` ni archivos `.db` (ver `.gitignore`).
