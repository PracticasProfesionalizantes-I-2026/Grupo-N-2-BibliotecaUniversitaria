# AGENTS.md — BiblioGest API

Contexto operativo para agentes de IA (Claude Code, Copilot, etc.) que
trabajen en este repositorio.

## Qué es este proyecto

API RESTful en .NET 10 con arquitectura N-Tier (`Controller → Service →
Repository → DbContext`) para BiblioGest, un sistema de gestión de
biblioteca universitaria (Práctica Profesionalizante I, Grupo N°2).

**Alcance actual (importante):** esta API implementa los 3 casos de uso
principales — Gestionar Libros, Gestionar Lectores y Gestionar Préstamos
(incluye el control de mora, que es una regla de negocio de Préstamos, no
una entidad aparte) —, Login/Autenticación (CU-01), Generar Reportes
(CU-07) y Gestionar Usuarios del Sistema (CU-08). También cumple los 4 niveles
de la matriz de complejidad avanzada de la cátedra: extravío con reposición del
ejemplar (1), suspensión por atraso (2), cola de reservas con pre-asignación de
24 h (3) y renovaciones limitadas (4).

Los casos de uso completos y las reglas de negocio (RN-01 a RN-21) están
documentados en `Documentos/Casos de Uso/`.

## Comandos

```bash
dotnet build BiblioGest.slnx      # compilar toda la solución
dotnet test BiblioGest.slnx       # correr unitarios + integración
dotnet run --project API          # levantar la API (Swagger UI en /swagger en Development)
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
  `DbContext`. Usa `AsNoTracking()` en toda lectura. Los Id son enteros
  autoincrementales que asigna la base al guardar (nunca los manda el caller).
- DTOs (`Shared/DTOs/`) son manuales, sin AutoMapper, separados en
  `CreateDTO`/`UpdateDTO`/`ResponseDTO` por entidad. Nunca se expone una
  entidad de EF directamente en una respuesta HTTP.
- Excepciones (`Shared/Exceptions/`) son tipadas, una clase por archivo,
  heredando de `NotFoundException`, `ValidationException`,
  `ConflictException` (409: duplicados, stock o intentos de borrar con
  dependencias activas) o `ForbiddenException` (403: el lector no puede pedir
  un préstamo por límite, mora o suspensión).

## Reglas de negocio implementadas

| Regla | Dónde se valida |
| --- | --- |
| Libro: título/autor/ISBN/ubicación obligatorios; título y autor de hasta 150 caracteres | `LibroService.CreateAsync/UpdateAsync` |
| Libro: stock entre 0 y 10 (más de 10 → `StockExcedidoException`) | `LibroService.CreateAsync/UpdateAsync` |
| Libro: ISBN de 10 o 13 dígitos (admite guiones; ISBN-10 puede terminar en X) | `LibroService.CreateAsync/UpdateAsync` |
| Libro: ubicación con formato Pasillo-Estantería-Estante (`A1-4`: A–J, 1–30, 1–6), se guarda en mayúsculas | `LibroService.CreateAsync/UpdateAsync` |
| Libro: ISBN único, comparado sin guiones (`IsbnDuplicadoException` → 409) | `LibroService.CreateAsync/UpdateAsync` + `LibroRepository.ExisteIsbnAsync` |
| Libro: no se elimina con préstamos activos o extraviados sin reponer | `LibroService.DeleteAsync` |
| Lector: nombre/apellido/email/DNI obligatorios | `LectorService.CreateAsync/UpdateAsync` |
| Lector: nombre y apellido de 2 a 50 caracteres, solo letras (un espacio, apóstrofo o guion entre palabras); email de 6 a 100 caracteres con formato válido | `LectorService.CreateAsync/UpdateAsync` |
| Lector: DNI de 7 u 8 dígitos, sin puntos ni espacios | `LectorService.CreateAsync/UpdateAsync` |
| Lector: DNI único (`DniDuplicadoException` → 409) | `LectorService.CreateAsync/UpdateAsync` |
| Lector: email único, sin distinguir mayúsculas (`LectorEmailDuplicadoException` → 409); se guarda en minúsculas | `LectorService.CreateAsync/UpdateAsync` + índice único |
| Lector: no se elimina con préstamos activos o extraviados sin reponer | `LectorService.DeleteAsync` |
| Préstamo: máximo 3 activos por lector (403) | `PrestamoService.CreateAsync` |
| Préstamo: no se presta un libro sin stock | `PrestamoService.CreateAsync` |
| Préstamo: lector en mora no puede pedir otro (403) | `PrestamoService.CreateAsync` |
| Préstamo: un lector no puede tener dos préstamos activos del mismo libro (409) | `PrestamoService.CreateAsync` |
| Préstamo: `GET /api/prestamos/estado-lector/{id}` anticipa si el lector puede pedir y el motivo, con las mismas reglas y mensajes que el alta | `PrestamoService.GetEstadoLectorAsync` |
| Préstamo: vencimiento a 14 días | `PrestamoService.CreateAsync` |
| Préstamo: solo se devuelve un préstamo `Activo`; devolución repetida o de un extraviado → 409 | `PrestamoService.RegistrarDevolucionAsync` |
| Préstamo: extravío (solo desde `Activo`) → estado `Extraviado`, el stock no vuelve y el lector queda bloqueado (403) hasta reponer el ejemplar; reposición (solo desde `Extraviado`) → `Repuesto`, stock +1, aplica suspensión si fue tarde. Sin multas monetarias (nivel 1) | `PrestamoService.RegistrarExtravioAsync/RegistrarReposicionAsync` + `CreateAsync` |
| Préstamo: devolución con atraso suspende al lector 1 día por día de atraso (se acumula); suspendido → 403 al pedir otro (nivel 2) | `PrestamoService.RegistrarDevolucionAsync` + `CreateAsync` (`Lector.SuspendidoHasta`) |
| Préstamo: "vencido" se calcula en la consulta, no se persiste | `PrestamoRepository.GetVencidosAsync` / `LectorTieneMoraAsync` |
| Login: credenciales inválidas o usuario inactivo → 401 (email sin distinguir mayúsculas) | `AuthService.LoginAsync` |
| Usuario: solo el Administrador gestiona usuarios (RN-19) | `[Authorize(Roles = "Administrador")]` en `UsuariosController` |
| Usuario: email/contraseña/rol obligatorios, rol válido (RN-20) | `UsuarioService.CreateAsync/UpdateAsync` |
| Usuario: contraseña de 6 a 64 caracteres, guardada con hash PBKDF2 (RN-21) | `UsuarioService` + `IPasswordHasher` |
| Usuario: email único, de 6 a 100 caracteres y con formato válido | `UsuarioService.CreateAsync/UpdateAsync` |
| Usuario: baja lógica (`Activo = false`) | `UsuarioService.DeleteAsync` |
| Modificación (PUT) de libros, lectores y usuarios: los campos que no se envían, van en `null` o vacíos mantienen su valor; los que traen un valor se validan como en el alta | `LibroService/LectorService/UsuarioService.UpdateAsync` |
| Usuario: el logueado no puede darse de baja, desactivarse ni quitarse el rol de Administrador; no se puede dejar al sistema sin administradores activos (409) | `UsuarioService.UpdateAsync/DeleteAsync` |
| Errores 401/403 del token y de model binding responden `{ "message" }` en español | `Program.cs` |
| Reserva: solo si el libro tiene stock 0 (si no, 409); una reserva activa por lector y libro (409); cola por orden de llegada | `ReservaService.CreateAsync` |
| Reserva: al devolver o reponer un ejemplar con cola, no vuelve al stock: se asigna al primero por 24 h (`Asignada`); solo ese lector puede retirarlo y la reserva pasa a `Concretada` | `ColaDeReservas.AsignarAlSiguienteAsync` + `PrestamoService.CreateAsync` |
| Reserva: si vence el plazo de 24 h queda `Vencida` y el ejemplar pasa al siguiente o vuelve al stock. Se evalúa en cada consulta u operación sobre el libro, sin tareas en segundo plano | `ColaDeReservas.ProcesarAsignacionesVencidasAsync` |
| Reserva: cancelar solo `EnCola`/`Asignada` (si no, 409); si estaba asignada libera el ejemplar | `ReservaService.CancelarAsync` |
| Libro y lector: no se eliminan con reservas activas ni con historial de préstamos, aunque estén todos devueltos (409, se conserva el historial) | `LibroService/LectorService.DeleteAsync` |
| Renovación: +14 días, máximo 2 (403); denegada si hay reservas activas del libro o el préstamo está vencido (403); solo préstamos `Activo` (409) | `PrestamoService.RenovarAsync` |
| Reportes: solo básicos — resumen (incluye extraviados y repuestos), préstamos por fechas, mora (RN-17/RN-18) | `ReporteService.GetReporteAsync` |
| Reportes: tipo inexistente, `desde` > `hasta` o rango mayor a un año → 400 | `ReporteService.GetReporteAsync` |
| Reportes: sin datos → 200 con lista vacía y mensaje | `ReporteService` |

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

- Login/Autenticación (CU-01) ya está implementado: Libros, Lectores, Reservas y
  Préstamos y Reportes llevan `[Authorize]` (JWT); `UsuariosController` lleva
  `[Authorize(Roles = "Administrador")]`; `AuthController` queda anónimo.
- Al agregar o cambiar una regla, actualizar `Documentos/Trazabilidad.md`.
- `bruno/` debe tener un request por endpoint nuevo que se agregue,
  cubriendo al menos un caso de éxito y los de error relevantes.
- No commitear `bin/`, `obj/` ni archivos `.db` (ver `.gitignore`).
