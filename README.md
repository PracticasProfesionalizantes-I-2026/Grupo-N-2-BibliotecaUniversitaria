# BiblioGest — Grupo N°2 Biblioteca Universitaria

Integrantes: Lautaro Navarro y Elías Korell

## Documentación

- Documentación general: https://docs.google.com/document/d/1l-hgM-n6PiBafRuCns1cQy5qqAaQTxlvS21iAa8c9ww/edit?tab=t.0
- Presentación de la app BiblioGest: https://1drv.ms/p/c/f7646867687d1003/IQCJK54DJMozRJ3uwkxZlD3hAWH1imrQJQQrl5GPhNG3fgY?e=jVX4eW
- Presentación de casos de uso: https://docs.google.com/document/d/1cPGvxmILelmMMogMiCeEFB1LkpB8VdPgWXqcZl9IrxY/edit?usp=sharing
- Mocap (Figma): https://www.figma.com/design/wegjr31mBIEI3hgwNyycAH/Mocap---BiblioGest?node-id=0-1&m=dev&t=xQyKmKHf32WRdCLE-1
- Casos de uso detallados: [`Documentos/Casos de Uso/`](Documentos/Casos%20de%20Uso/)

## Alcance de esta entrega

BiblioGest es un sistema de gestión de biblioteca universitaria. Esta API
cubre exclusivamente los 3 casos de uso principales del sistema —
**Gestionar Libros**, **Gestionar Lectores** y **Gestionar Préstamos**
(incluyendo el control de mora, que es parte de la misma regla de negocio
de Préstamos) — según lo acordado con la cátedra.

Además están implementados **Iniciar Sesión** (CU-01), **Generar
Reportes** (CU-07) y **Gestionar Usuarios del Sistema** (CU-08). Los
endpoints de Libros, Lectores, Préstamos y Reportes requieren un token JWT
(`Authorization: Bearer <token>`, obtenido via `POST /api/auth/login`) y
llevan `[Authorize]`; los de Usuarios llevan
`[Authorize(Roles = "Administrador")]` (RN-19).

## Arquitectura

API RESTful en .NET 10 con arquitectura en capas (N-Tier), siguiendo el
flujo obligatorio:

```
Controller → Service → Repository → DbContext
```

```
BiblioGest.slnx
API/                → BiblioGest.Api
  Controllers/        AuthController, LibrosController, LectoresController, PrestamosController, ReportesController, UsuariosController
  Middleware/         ExceptionMiddleware (errores no controlados → 500)
  Program.cs          DI, DbContext, DbInitializer, Scalar (OpenAPI UI)
BusinessLogic/      → BiblioGest.BusinessLogic
  Interfaces/         IAuthService, ILibroService, ILectorService, IPrestamoService, IReporteService, IUsuarioService, IPasswordHasher
  Services/           AuthService, LibroService, LectorService, PrestamoService, ReporteService, UsuarioService, PasswordHasher
DataAccess/         → BiblioGest.DataAccess (EF Core + SQLite)
  Entities/           Libro, Lector, Prestamo, EstadoPrestamo, Usuario, RolUsuario
  Repositories/       ILibroRepository/LibroRepository, etc.
  BiblioGestDbContext.cs
  DbInitializer.cs    Crea la base y carga datos de prueba
  Migrations/
Shared/             → BiblioGest.Shared
  DTOs/               Create/Update/Response por entidad
  Exceptions/         NotFoundException, ValidationException, ConflictException + concretas
bruno/              → Colección de requests (Auth, Libros, Lectores, Prestamos, Reportes, Usuarios)
tests/
  BiblioGest.UnitTests/        xUnit + Moq (services, sin tocar la base)
  BiblioGest.IntegrationTests/ WebApplicationFactory (API completa, SQLite en memoria)
```

**Responsabilidades por capa:**

| Capa | Responsabilidad |
| --- | --- |
| API | Recibe HTTP, valida el DTO de entrada, mapea excepciones a status codes con `try/catch` |
| BusinessLogic | Reglas de negocio, orquestación, validaciones, `MapToResponseDTO` manual |
| DataAccess | Único acceso a la base vía EF Core (`AsNoTracking()` en lecturas, Guid asignado en `CreateAsync`) |
| Shared | DTOs y excepciones tipadas, compartidos entre capas |

Los services reciben sus repositorios (interfaces de `DataAccess`) por
constructor — nunca los instancian directamente — lo que permite mockearlos
con Moq en los tests unitarios.

## Entidades

- **Libro**: Id (Guid), Titulo, Autor, Isbn, Ubicacion, Stock (int, ≥ 0).
- **Lector**: Id (Guid), Nombre, Apellido, Email, Identificador (único — DNI
  o legajo en un solo campo).
- **Prestamo**: Id (Guid), LectorId, LibroId, FechaPrestamo,
  FechaVencimiento (FechaPrestamo + 14 días), FechaDevolucion (nullable),
  Estado (Activo/Devuelto — "vencido" se calcula en la consulta, no se
  persiste).
- **Usuario**: Id (Guid), Email (único), PasswordHash (PBKDF2), RolUsuario
  (Administrador/Bibliotecario), Activo (baja lógica).

`Prestamo` es hijo de `Libro` y `Lector` con `DeleteBehavior.Restrict`: no
se puede eliminar un libro o lector con préstamos activos a nivel de base
de datos, además de la validación explícita en el service (409 Conflict).

## Reglas de negocio y excepciones

| Regla | Excepción (`Shared/Exceptions/`) | HTTP |
| --- | --- | --- |
| Datos obligatorios del libro faltantes | `LibroInvalidoException` | 400 |
| Stock negativo | `LibroInvalidoException` | 400 |
| Datos obligatorios del lector faltantes | `LectorInvalidoException` | 400 |
| Libro/Lector/Préstamo inexistente | `LibroNotFoundException` / `LectorNotFoundException` / `PrestamoNotFoundException` | 404 |
| Eliminar libro con préstamos activos | `LibroConPrestamosActivosException` | 409 |
| Eliminar lector con préstamos activos | `LectorConPrestamosActivosException` | 409 |
| Identificador (DNI/legajo) duplicado | `IdentificadorDuplicadoException` | 409 |
| Lector con 3 préstamos activos | `LimitePrestamosActivosException` | 409 |
| Libro sin stock disponible | `StockInsuficienteException` | 409 |
| Lector con préstamos vencidos (mora) | `LectorEnMoraException` | 409 |
| Devolver un préstamo ya devuelto | `PrestamoYaDevueltoException` | 409 |
| Credenciales inválidas o usuario inactivo (login) | `CredencialesInvalidasException` | 401 |
| Usuario sin email, contraseña < 6 caracteres o rol inexistente (RN-20, RN-21) | `UsuarioInvalidoException` | 400 |
| Usuario inexistente | `UsuarioNotFoundException` | 404 |
| Email de usuario duplicado | `UsuarioDuplicadoException` | 409 |
| Reporte inexistente o filtro de fechas inválido (`desde` > `hasta`) | `ReporteInvalidoException` | 400 |
| Usuario sin rol Administrador en `/api/usuarios` (RN-19) | `[Authorize(Roles)]` | 403 |

Todas heredan de una categoría base (`NotFoundException`,
`ValidationException`, `ConflictException`), y cada controller las mapea
con bloques `try/catch` explícitos. Cualquier otra excepción no prevista la
captura `ExceptionMiddleware`, que la registra en el log y responde `500`
con `{ "message": "Ocurrió un error interno en el servidor." }`.

## Cómo ejecutar

Desde la raíz del repositorio:

```bash
dotnet build BiblioGest.slnx      # compila toda la solución
dotnet test BiblioGest.slnx       # corre unitarios (Moq) e integración (WebApplicationFactory)
dotnet run --project API          # levanta la API (crea bibliogest.db con datos de prueba)
```

Con la API corriendo en desarrollo, la documentación interactiva está en:

```
http://localhost:<puerto>/scalar/v1
```

La base SQLite (`bibliogest.db`) se crea sola la primera vez que se corre
la API, con datos de prueba (3 libros, 2 lectores, 1 préstamo vencido) vía
`DataAccess/DbInitializer.cs`.

## Catálogo de endpoints

### Auth

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/login` | Iniciar sesión, devuelve el token JWT | 200 | 400, 401 |

Usuarios de prueba: `admin@bibliogest.com` / `Admin123!` (Administrador) y
`bibliotecario@bibliogest.com` / `Bibliotecario123!` (Bibliotecario).

### Libros

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/libros?busqueda=` | Listar/buscar por título, autor o ISBN | 200 | — |
| GET | `/api/libros/{id}` | Obtener un libro | 200 | 404 |
| POST | `/api/libros` | Crear libro | 201 | 400 |
| PUT | `/api/libros/{id}` | Modificar libro | 200 | 400, 404 |
| DELETE | `/api/libros/{id}` | Eliminar libro | 200 | 404, 409 |

Ejemplo `POST /api/libros`:

```json
// Request
{
  "titulo": "El Aleph",
  "autor": "Jorge Luis Borges",
  "isbn": "978-8420633109",
  "ubicacion": "Estante A4",
  "stock": 4
}

// Response 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "titulo": "El Aleph",
  "autor": "Jorge Luis Borges",
  "isbn": "978-8420633109",
  "ubicacion": "Estante A4",
  "stock": 4,
  "disponible": true
}
```

### Lectores

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/lectores` | Listar lectores | 200 | — |
| GET | `/api/lectores/{id}` | Obtener un lector | 200 | 404 |
| POST | `/api/lectores` | Crear lector | 201 | 400, 409 |
| PUT | `/api/lectores/{id}` | Modificar lector | 200 | 400, 404, 409 |
| DELETE | `/api/lectores/{id}` | Eliminar lector | 200 | 404, 409 |

Ejemplo `POST /api/lectores`:

```json
// Request
{
  "nombre": "Facundo",
  "apellido": "López",
  "email": "facundo.lopez@example.com",
  "identificador": "35555111"
}

// Response 201 Created
{
  "id": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "nombre": "Facundo",
  "apellido": "López",
  "email": "facundo.lopez@example.com",
  "identificador": "35555111"
}
```

### Préstamos

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| POST | `/api/prestamos` | Crear préstamo | 201 | 404, 409 |
| GET | `/api/prestamos/{id}` | Obtener un préstamo | 200 | 404 |
| PUT | `/api/prestamos/{id}/devolucion` | Registrar devolución | 200 | 404, 409 |
| GET | `/api/prestamos/mora` | Listar préstamos vencidos | 200 | — |

Ejemplo `POST /api/prestamos`:

```json
// Request
{
  "lectorId": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "libroId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}

// Response 201 Created
{
  "id": "c9d8e7f6-a5b4-3210-9876-543210fedcba",
  "lectorId": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "lectorNombreCompleto": "Facundo López",
  "libroId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "libroTitulo": "El Aleph",
  "fechaPrestamo": "2026-09-18T01:00:00Z",
  "fechaVencimiento": "2026-10-02T01:00:00Z",
  "fechaDevolucion": null,
  "estado": "Activo",
  "enMora": false
}
```

### Reportes

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/reportes` | Listar los reportes disponibles | 200 | 401 |
| GET | `/api/reportes/resumen` | Totales de libros, lectores y préstamos por estado | 200 | 401 |
| GET | `/api/reportes/prestamos?desde=&hasta=` | Préstamos en un rango de fechas | 200 | 400, 401 |
| GET | `/api/reportes/mora` | Préstamos activos vencidos | 200 | 401 |

Si no hay datos, el reporte vuelve con `200`, la lista vacía y
`"mensaje": "No hay datos disponibles para el reporte."`.

### Usuarios (solo Administrador)

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/usuarios` | Listar usuarios | 200 | 401, 403 |
| GET | `/api/usuarios/{id}` | Obtener un usuario | 200 | 401, 403, 404 |
| POST | `/api/usuarios` | Crear usuario | 201 | 400, 401, 403, 409 |
| PUT | `/api/usuarios/{id}` | Modificar usuario (contraseña opcional) | 200 | 400, 401, 403, 404, 409 |
| DELETE | `/api/usuarios/{id}` | Baja lógica (`activo = false`) | 200 | 401, 403, 404 |

Ejemplo `POST /api/usuarios`:

```json
// Request
{
  "email": "nuevo.bibliotecario@bibliogest.com",
  "password": "Clave123",
  "rolUsuario": "Bibliotecario"
}

// Response 201 Created
{
  "id": "5d1c7e2a-9b3f-4a61-8e0d-2f6b7c9a1e44",
  "email": "nuevo.bibliotecario@bibliogest.com",
  "rolUsuario": "Bibliotecario",
  "activo": true
}
```

## Colección de Bruno

En `bruno/` hay una carpeta por controller con un request por caso de éxito
y de error relevante (400/401/403/404/409). El entorno `Local` trae `baseUrl` y los
requests de creación completan automáticamente `libroId`/`lectorId`/
`prestamoId`/`usuarioId` para encadenar los siguientes. Primero hay que
ejecutar `Auth/Login Administrador (200)`, que guarda el `token`.

## Testing

- `tests/BiblioGest.UnitTests`: xUnit + Moq sobre los services, sin tocar
  la base real (50 tests).
- `tests/BiblioGest.IntegrationTests`: `WebApplicationFactory` levantando la
  API completa contra una SQLite en memoria aislada por instancia (49 tests).

Link: https://docs.google.com/document/d/1f4idud-PCBsQK\_48ZCf5ibF\_MNzNLKdxSAvWgO32Wt4/edit?tab=t.0#heading=h.frnk5y8ou0gl
