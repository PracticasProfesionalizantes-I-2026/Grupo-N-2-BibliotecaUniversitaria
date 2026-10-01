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
cubre **Gestionar Libros**, **Gestionar Lectores**, **Gestionar Préstamos**
(incluyendo el control de mora, que es parte de la misma regla de negocio de
Préstamos), **Gestionar Usuarios del Sistema** (CU-08) e **Iniciar Sesión**
(CU-01, login con JWT).

**Fuera de esta entrega** (a implementar por el equipo): Generar Reportes
(CU-07).

Todos los endpoints salvo `POST /api/v1/auth/login` requieren un token Bearer
(`[Authorize]`); los de Usuarios, además, solo son accesibles para el rol
Administrador (`[Authorize(Roles = "Administrador")]`). Ver la sección
[Autenticación](#autenticación) para más detalle.

## Arquitectura

API RESTful en .NET 10 con arquitectura en capas (N-Tier), siguiendo el
flujo obligatorio:

```
Controller → Service → Repository → DbContext
```

```
BiblioGest.slnx
API/                → BiblioGest.Api
  Controllers/        LibrosController, LectoresController, PrestamosController,
                       UsuariosController, AuthController
  ExceptionHandling/  GlobalExceptionHandler (IExceptionHandler → ProblemDetails)
  Program.cs          DI, DbContext, DbInitializer, JWT, Scalar (OpenAPI UI)
BusinessLogic/      → BiblioGest.BusinessLogic
  Interfaces/         ILibroService, ILectorService, IPrestamoService,
                       IUsuarioService, IAuthService
  Services/           LibroService, LectorService, PrestamoService,
                       UsuarioService, AuthService
DataAccess/         → BiblioGest.DataAccess (EF Core + SQLite)
  Entities/           Libro, Lector, Prestamo, EstadoPrestamo, Usuario, RolUsuario
  Repositories/       ILibroRepository/LibroRepository, etc.
  BiblioGestDbContext.cs
  DbInitializer.cs    Crea la base y carga datos de prueba (incluye un Administrador)
  Migrations/
Shared/             → BiblioGest.Shared
  DTOs/               Create/Update/Response por entidad + Auth (Login)
  Exceptions/         NotFoundException, ValidationException, ConflictException,
                       UnauthorizedException + concretas
bruno/              → Colección de requests (Auth, Libros, Lectores, Prestamos, Usuarios)
tests/
  BiblioGest.UnitTests/        xUnit + Moq (services, sin tocar la base)
  BiblioGest.IntegrationTests/ WebApplicationFactory (API completa, SQLite en memoria)
```

**Responsabilidades por capa:**

| Capa | Responsabilidad |
| --- | --- |
| API | Recibe HTTP, valida el DTO de entrada; las excepciones las traduce a `ProblemDetails` un manejador global (`GlobalExceptionHandler`), sin `try/catch` en los controllers |
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
- **Usuario**: Id (Guid), Nombre, Email (único), PasswordHash, Rol
  (Bibliotecario/Administrador), Activo (la baja es lógica, no se borra el
  registro).

`Prestamo` es hijo de `Libro` y `Lector` con `DeleteBehavior.Restrict`: no
se puede eliminar un libro o lector con préstamos activos a nivel de base
de datos, además de la validación explícita en el service (409 Conflict).

## Reglas de negocio y excepciones

| Regla | Excepción (`Shared/Exceptions/`) | HTTP |
| --- | --- | --- |
| Datos obligatorios del libro faltantes | `LibroInvalidoException` | 400 |
| Stock negativo | `LibroInvalidoException` | 400 |
| Datos obligatorios del lector faltantes | `LectorInvalidoException` | 400 |
| Datos obligatorios del usuario faltantes / contraseña < 6 caracteres | `UsuarioInvalidoException` | 400 |
| Libro/Lector/Préstamo/Usuario inexistente | `LibroNotFoundException` / `LectorNotFoundException` / `PrestamoNotFoundException` / `UsuarioNotFoundException` | 404 |
| Eliminar libro con préstamos activos | `LibroConPrestamosActivosException` | 409 |
| Eliminar lector con préstamos activos | `LectorConPrestamosActivosException` | 409 |
| Identificador (DNI/legajo) duplicado | `IdentificadorDuplicadoException` | 409 |
| Email de usuario duplicado | `EmailDuplicadoException` | 409 |
| Lector con 3 préstamos activos | `LimitePrestamosActivosException` | 409 |
| Libro sin stock disponible | `StockInsuficienteException` | 409 |
| Lector con préstamos vencidos (mora) | `LectorEnMoraException` | 409 |
| Usuario inexistente/inactivo o contraseña incorrecta en el login | `CredencialesInvalidasException` | 401 |

Todas heredan de una categoría base (`NotFoundException`, `ValidationException`,
`ConflictException`, `UnauthorizedException`), que el `GlobalExceptionHandler`
traduce a `ProblemDetails` sin necesidad de `try/catch` en los controllers.

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
la API, con datos de prueba (3 libros, 2 lectores, 1 préstamo vencido, 1
usuario Administrador) vía `DataAccess/DbInitializer.cs`.

## Autenticación

La API usa JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`). Para
obtener un token:

```
POST /api/v1/auth/login
{
  "email": "admin@bibliogest.local",
  "password": "Admin123!"
}
```

El Administrador de arriba lo siembra `DbInitializer` (solo desarrollo, no
usar en producción). La respuesta incluye el token (con el rol como claim) y
su expiración; hay que mandarlo como `Authorization: Bearer <token>` en el
resto de los endpoints, salvo el propio login.

- En Scalar (`/scalar/v1`) el esquema `Bearer` está configurado como
  seguridad global: hay un botón "Authorize" para pegar el token una vez y
  que se mande automáticamente en todos los requests de prueba.
- En Bruno, correr primero `Auth/Login (200)`: guarda el token en la
  variable de entorno `token`, que el resto de la colección usa como
  `auth: bearer`.
- Los endpoints de Usuarios además requieren rol `Administrador`
  (`[Authorize(Roles = "Administrador")]`); un token con rol `Bibliotecario`
  recibe 403 Forbidden.

## Catálogo de endpoints

> Todos los endpoints de abajo requieren token Bearer, salvo
> `POST /api/v1/auth/login`. Los de Usuarios requieren además rol
> Administrador.

### Auth

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| POST | `/api/v1/auth/login` | Iniciar sesión | 200 | 400, 401 |

Ejemplo `POST /api/v1/auth/login`:

```json
// Request
{
  "email": "admin@bibliogest.local",
  "password": "Admin123!"
}

// Response 200 OK
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "nombre": "Administrador BiblioGest",
  "email": "admin@bibliogest.local",
  "rol": "Administrador",
  "expira_en": "2026-10-01T18:00:00Z"
}
```

### Libros

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/v1/libros?busqueda=` | Listar/buscar por título, autor o ISBN | 200 | — |
| GET | `/api/v1/libros/{id}` | Obtener un libro | 200 | 404 |
| POST | `/api/v1/libros` | Crear libro | 201 | 400 |
| PUT | `/api/v1/libros/{id}` | Modificar libro | 200 | 400, 404 |
| DELETE | `/api/v1/libros/{id}` | Eliminar libro | 200 | 404, 409 |

Ejemplo `POST /api/v1/libros`:

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
| GET | `/api/v1/lectores` | Listar lectores | 200 | — |
| GET | `/api/v1/lectores/{id}` | Obtener un lector | 200 | 404 |
| POST | `/api/v1/lectores` | Crear lector | 201 | 400, 409 |
| PUT | `/api/v1/lectores/{id}` | Modificar lector | 200 | 400, 404, 409 |
| DELETE | `/api/v1/lectores/{id}` | Eliminar lector | 200 | 404, 409 |

Ejemplo `POST /api/v1/lectores`:

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
| POST | `/api/v1/prestamos` | Crear préstamo | 201 | 404, 409 |
| GET | `/api/v1/prestamos/{id}` | Obtener un préstamo | 200 | 404 |
| PUT | `/api/v1/prestamos/{id}/devolucion` | Registrar devolución | 200 | 404 |
| GET | `/api/v1/prestamos/mora` | Listar préstamos vencidos | 200 | — |

Ejemplo `POST /api/v1/prestamos`:

```json
// Request
{
  "lector_id": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "libro_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}

// Response 201 Created
{
  "id": "c9d8e7f6-a5b4-3210-9876-543210fedcba",
  "lector_id": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "lector_nombre_completo": "Facundo López",
  "libro_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "libro_titulo": "El Aleph",
  "fecha_prestamo": "2026-09-18T01:00:00Z",
  "fecha_vencimiento": "2026-10-02T01:00:00Z",
  "fecha_devolucion": null,
  "estado": "Activo",
  "en_mora": false
}
```

### Usuarios

Requiere rol Administrador.

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/v1/usuarios` | Listar usuarios | 200 | — |
| GET | `/api/v1/usuarios/{id}` | Obtener un usuario | 200 | 404 |
| POST | `/api/v1/usuarios` | Crear usuario | 201 | 400, 409 |
| PUT | `/api/v1/usuarios/{id}` | Modificar usuario | 200 | 400, 404, 409 |
| DELETE | `/api/v1/usuarios/{id}` | Baja lógica (`Activo = false`) | 204 | 404 |

Ejemplo `POST /api/v1/usuarios`:

```json
// Request
{
  "nombre": "Carla Pérez",
  "email": "carla.perez@example.com",
  "password": "secreta1",
  "rol": "Bibliotecario"
}

// Response 201 Created
{
  "id": "d4c3b2a1-1234-5678-9abc-def012345678",
  "nombre": "Carla Pérez",
  "email": "carla.perez@example.com",
  "rol": "Bibliotecario",
  "activo": true
}
```

El `password` nunca se devuelve en las respuestas; se guarda hasheado con
`PasswordHasher<Usuario>` de ASP.NET Core Identity.

### Formato de errores

Los endpoints devuelven los errores como [`ProblemDetails`](https://datatracker.ietf.org/doc/html/rfc7807)
(`application/problem+json`) para los códigos 400, 401, 404 y 409, generados por un
manejador global de excepciones (`API/ExceptionHandling/GlobalExceptionHandler.cs`)
que traduce las excepciones tipadas de `Shared/Exceptions/` sin necesidad de
`try/catch` en los controllers. Los errores de validación de esquema (campos
obligatorios, formato de email, longitud máxima) se devuelven como
`ValidationProblemDetails` 400 generado automáticamente a partir de las
`DataAnnotations` de los DTOs de `Shared/DTOs/`.

Ejemplo de error 404:

```json
{
  "status": 404,
  "title": "Recurso no encontrado",
  "detail": "No se encontró el libro con Id '3fa85f64-5717-4562-b3fc-2c963f66afa6'."
}
```

## Colección de Bruno

En `bruno/` hay una carpeta por controller (`Auth`, `Libros`, `Lectores`,
`Prestamos`, `Usuarios`) con un request por caso de éxito y de error
relevante (400/401/403/404/409). El entorno `Local` trae `baseUrl` y los
requests de creación completan automáticamente `libroId`/`lectorId`/
`prestamoId`/`usuarioId` para encadenar los siguientes.

Antes de probar cualquier endpoint que no sea login, correr
`Auth/Login (200)`: guarda el token en la variable `token`, que el resto de
la colección usa como `auth: bearer`.

## Testing

- `tests/BiblioGest.UnitTests`: xUnit + Moq sobre los services, sin tocar
  la base real (34 tests).
- `tests/BiblioGest.IntegrationTests`: `WebApplicationFactory` levantando la
  API completa contra una SQLite en memoria aislada por instancia (43 tests).
