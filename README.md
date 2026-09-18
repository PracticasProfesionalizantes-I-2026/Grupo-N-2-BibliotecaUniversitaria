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

**Fuera de esta entrega** (a implementar por el equipo): Login/Autenticación
(CU-01), Generar Reportes (CU-07) y Gestionar Usuarios del Sistema (CU-08).
Por eso los endpoints actuales no requieren token ni llevan `[Authorize]`.

## Arquitectura

API RESTful en .NET 10 con arquitectura en capas (N-Tier), siguiendo el
flujo obligatorio:

```
Controller → Service → Repository → DbContext
```

```
BiblioGest.slnx
API/                → BiblioGest.Api
  Controllers/        LibrosController, LectoresController, PrestamosController
  Program.cs          DI, DbContext, DbInitializer, Scalar (OpenAPI UI)
BusinessLogic/      → BiblioGest.BusinessLogic
  Interfaces/         ILibroService, ILectorService, IPrestamoService
  Services/           LibroService, LectorService, PrestamoService
DataAccess/         → BiblioGest.DataAccess (EF Core + SQLite)
  Entities/           Libro, Lector, Prestamo, EstadoPrestamo
  Repositories/       ILibroRepository/LibroRepository, etc.
  BiblioGestDbContext.cs
  DbInitializer.cs    Crea la base y carga datos de prueba
  Migrations/
Shared/             → BiblioGest.Shared
  DTOs/               Create/Update/Response por entidad
  Exceptions/         NotFoundException, ValidationException, ConflictException + concretas
bruno/              → Colección de requests (Libros, Lectores, Prestamos)
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

Todas heredan de una categoría base (`NotFoundException`,
`ValidationException`, `ConflictException`), y cada controller las mapea
con bloques `try/catch` explícitos.

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
| PUT | `/api/prestamos/{id}/devolucion` | Registrar devolución | 200 | 404 |
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

## Colección de Bruno

En `bruno/` hay una carpeta por controller con un request por caso de éxito
y de error relevante (400/404/409). El entorno `Local` trae `baseUrl` y los
requests de creación completan automáticamente `libroId`/`lectorId`/
`prestamoId` para encadenar los siguientes.

## Testing

- `tests/BiblioGest.UnitTests`: xUnit + Moq sobre los 3 services, sin tocar
  la base real (20 tests).
- `tests/BiblioGest.IntegrationTests`: `WebApplicationFactory` levantando la
  API completa contra una SQLite en memoria aislada por instancia (12 tests).
