# BiblioGest — Grupo N°2 Biblioteca Universitaria

Integrantes: Lautaro Navarro y Elías Korell

## Documentación

- Documentación general: https://docs.google.com/document/d/1l-hgM-n6PiBafRuCns1cQy5qqAaQTxlvS21iAa8c9ww/edit?tab=t.0
- Presentación de la app BiblioGest: https://1drv.ms/p/c/f7646867687d1003/IQCJK54DJMozRJ3uwkxZlD3hAWH1imrQJQQrl5GPhNG3fgY?e=jVX4eW
- Presentación de casos de uso: https://docs.google.com/document/d/1cPGvxmILelmMMogMiCeEFB1LkpB8VdPgWXqcZl9IrxY/edit?usp=sharing
- Mocap (Figma): https://www.figma.com/design/wegjr31mBIEI3hgwNyycAH/Mocap---BiblioGest?node-id=0-1&m=dev&t=xQyKmKHf32WRdCLE-1
- Casos de uso detallados: [`Documentos/Casos de Uso/`](Documentos/Casos%20de%20Uso/)
- Matriz de trazabilidad (RN → CU → código → tests → Bruno): [`Documentos/Trazabilidad.md`](Documentos/Trazabilidad.md)

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

**Matriz de complejidad avanzada (cumple los 4 niveles):**

1. **Estados y máquinas de flujo:** un préstamo puede pasar a `Extraviado`; el lector
   queda bloqueado (403) hasta reponer el mismo ejemplar (estado `Repuesto`). Se eligió
   reposición en lugar de multa monetaria (RN-16: sin multas).
2. **Tiempos y caducidad:** devolver con atraso suspende al lector 1 día por cada día
   de retraso; mientras dure, pedir un préstamo responde 403.
3. **Cola de reservas:** con stock 0 se puede reservar en cola; al devolverse un
   ejemplar se pre-asigna automáticamente 24 h al primero de la cola. Si no lo retira,
   pasa al siguiente o vuelve al stock (se evalúa en cada consulta, igual que la mora).
4. **Renovaciones:** máximo 2 renovaciones de 14 días cada una, denegadas (403) si hay
   lectores activos en la cola de reservas del libro o si el préstamo está vencido.

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
  Program.cs          DI, DbContext, DbInitializer, Swagger UI (OpenAPI)
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
  Exceptions/         NotFoundException, ValidationException, ConflictException, ForbiddenException + concretas
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
| DataAccess | Único acceso a la base vía EF Core (`AsNoTracking()` en lecturas, Id entero autoincremental que asigna la base) |
| Shared | DTOs y excepciones tipadas, compartidos entre capas |

Los services reciben sus repositorios (interfaces de `DataAccess`) por
constructor — nunca los instancian directamente — lo que permite mockearlos
con Moq en los tests unitarios.

## Entidades

- **Libro**: Id (int autoincremental), Titulo y Autor (hasta 150 caracteres), Isbn (único, 10 o 13 dígitos, admite guiones), Ubicacion (formato Pasillo-Estantería-Estante, ej. `A1-4`: pasillo A–J, estantería 1–30, estante 1–6), Stock (int, de 0 a 10).
- **Lector**: Id (int autoincremental), Nombre y Apellido (2 a 50 caracteres, solo letras), Email (6 a 100 caracteres, formato válido), Dni (único, 7 u 8 dígitos sin puntos), SuspendidoHasta (nullable: fecha hasta la que no puede pedir préstamos por haber devuelto con atraso).
- **Prestamo**: Id (int autoincremental), LectorId, LibroId, FechaPrestamo,
  FechaVencimiento (FechaPrestamo + 14 días), FechaDevolucion (nullable),
  Renovaciones (0 a 2), Estado (Activo/Devuelto/Extraviado/Repuesto — "vencido" se calcula
  en la consulta, no se persiste).
- **Reserva**: Id (int autoincremental), LectorId, LibroId, FechaReserva, FechaAsignacion y
  FechaLimiteRetiro (nullable, asignación + 24 h), Estado (EnCola/Asignada/Concretada/
  Vencida/Cancelada).
- **Usuario**: Id (int autoincremental), Email (único, 6 a 100 caracteres, formato válido), PasswordHash (PBKDF2), RolUsuario
  (Administrador/Bibliotecario), Activo (baja lógica).

`Prestamo` es hijo de `Libro` y `Lector` con `DeleteBehavior.Restrict`: no
se puede eliminar un libro o lector con préstamos activos a nivel de base
de datos, además de la validación explícita en el service (409 Conflict).

## Reglas de negocio y excepciones

| Regla | Excepción (`Shared/Exceptions/`) | HTTP |
| --- | --- | --- |
| Datos obligatorios del libro faltantes | `LibroInvalidoException` | 400 |
| Stock negativo | `LibroInvalidoException` | 400 |
| Stock mayor a 10 ejemplares | `StockExcedidoException` | 400 |
| ISBN sin 10 o 13 dígitos, ubicación fuera del formato `A1-4`, o título/autor de más de 150 caracteres | `LibroInvalidoException` | 400 |
| ISBN ya registrado en otro libro | `IsbnDuplicadoException` | 409 |
| Datos obligatorios del lector faltantes, email inválido, DNI que no tenga 7 u 8 dígitos, nombre/apellido fuera de 2 a 50 caracteres o con caracteres que no sean letras, o email fuera de 6 a 100 caracteres | `LectorInvalidoException` | 400 |
| Libro/Lector/Préstamo inexistente | `LibroNotFoundException` / `LectorNotFoundException` / `PrestamoNotFoundException` | 404 |
| Eliminar libro con préstamos activos o extraviados sin reponer | `LibroConPrestamosActivosException` | 409 |
| Eliminar lector con préstamos activos o extraviados sin reponer | `LectorConPrestamosActivosException` | 409 |
| DNI de lector duplicado | `DniDuplicadoException` | 409 |
| Lector con 3 préstamos activos | `LimitePrestamosActivosException` | 403 |
| Libro sin stock disponible | `StockInsuficienteException` | 409 |
| Lector con préstamos vencidos (mora) | `LectorEnMoraException` | 403 |
| Lector suspendido por devolver con atraso (1 día por día de atraso) | `LectorSuspendidoException` | 403 |
| Devolver un préstamo ya devuelto | `PrestamoYaDevueltoException` | 409 |
| Devolver o marcar extraviado un préstamo que no está activo | `PrestamoNoActivoException` | 409 |
| Reponer un préstamo que no está extraviado | `PrestamoNoExtraviadoException` | 409 |
| Lector con un libro extraviado sin reponer pide otro préstamo | `LectorConLibroExtraviadoException` | 403 |
| Reserva inexistente | `ReservaNotFoundException` | 404 |
| Reservar un libro que tiene stock (se presta directo) | `ReservaConStockDisponibleException` | 409 |
| Reservar dos veces el mismo libro | `ReservaDuplicadaException` | 409 |
| Cancelar una reserva que no está EnCola ni Asignada | `ReservaNoActivaException` | 409 |
| Eliminar libro o lector con reservas activas | `LibroConReservasActivasException` / `LectorConReservasActivasException` | 409 |
| Eliminar libro o lector que tiene historial de préstamos (aunque estén devueltos) | `LibroConHistorialException` / `LectorConHistorialException` | 409 |
| Email de lector ya usado por otro lector (sin distinguir mayúsculas) | `LectorEmailDuplicadoException` | 409 |
| Prestar a un lector un libro que ya tiene prestado | `PrestamoActivoDuplicadoException` | 409 |
| El administrador se da de baja, se desactiva o se quita el rol a sí mismo | `UsuarioAutoBajaException` / `UsuarioAutoModificacionException` | 409 |
| Dejar al sistema sin ningún administrador activo | `UltimoAdministradorException` | 409 |
| Request sin token o con token inválido/vencido | `JwtBearerEvents.OnChallenge` | 401 (`Debe iniciar sesión.` / `Token inválido o vencido.`) |
| Rol insuficiente | `JwtBearerEvents.OnForbidden` | 403 (`No tiene permisos para realizar esta acción.`) |
| Cuerpo vacío o JSON mal formado | `InvalidModelStateResponseFactory` | 400 con `{ "message" }` en español |
| Renovar un préstamo ya renovado 2 veces | `LimiteRenovacionesException` | 403 |
| Renovar con lectores en la cola de reservas del libro | `RenovacionConReservasException` | 403 |
| Renovar un préstamo vencido | `PrestamoVencidoNoRenovableException` | 403 |
| Credenciales inválidas o usuario inactivo (login) | `CredencialesInvalidasException` | 401 |
| Usuario sin email, email inválido o fuera de 6 a 100 caracteres, contraseña de menos de 6 o más de 64 caracteres, o rol inexistente (RN-20, RN-21) | `UsuarioInvalidoException` | 400 |
| Usuario inexistente | `UsuarioNotFoundException` | 404 |
| Email de usuario duplicado | `UsuarioDuplicadoException` | 409 |
| Reporte inexistente o filtro de fechas inválido (`desde` > `hasta` o rango mayor a un año) | `ReporteInvalidoException` | 400 |
| Usuario sin rol Administrador en `/api/usuarios` (RN-19) | `[Authorize(Roles)]` | 403 |

Todas heredan de una categoría base (`NotFoundException`,
`ValidationException`, `ConflictException`, `ForbiddenException`), y cada controller las mapea
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

Con la API corriendo en desarrollo (`dotnet run --project API` la abre sola
en el navegador), la documentación interactiva está en:

```
http://localhost:5203/swagger
```

### Cuentas de prueba

Se crean solas al levantar la API por primera vez:

| Rol | Email | Contraseña | Puede usar |
| --- | --- | --- | --- |
| Administrador | `admin@bibliogest.com` | `Admin123!` | Todos los endpoints, incluido `/api/usuarios` |
| Bibliotecario | `bibliotecario@bibliogest.com` | `Bibliotecario123!` | Todo menos `/api/usuarios` (responde `403`) |

### Guía rápida para probar desde Swagger

1. Ejecutar `dotnet run --project API` (requiere el SDK de .NET 10). Se abre
   `http://localhost:5203/swagger` en el navegador.
2. Abrir `POST /api/auth/login`, tocar **Try it out** y enviar:

   ```json
   {
     "email": "admin@bibliogest.com",
     "password": "Admin123!"
   }
   ```

3. Copiar el valor de `token` de la respuesta (sin comillas).
4. Tocar el botón **Authorize** (arriba a la derecha), pegar el token y
   confirmar. A partir de ahí, todos los requests lo envían en el header
   `Authorization: Bearer <token>`.
5. Probar cualquier endpoint, por ejemplo `GET /api/libros` (200) o
   `GET /api/prestamos/mora` (devuelve el préstamo vencido de prueba).

Para ver el `403` de RN-19, repetir los pasos con la cuenta de
Bibliotecario y llamar a `GET /api/usuarios`.

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
| POST | `/api/libros` | Crear libro | 201 | 400, 409 |
| PUT | `/api/libros/{id}` | Modificar libro | 200 | 400, 404, 409 |
| DELETE | `/api/libros/{id}` | Eliminar libro | 200 | 404, 409 |

Ejemplo `POST /api/libros`:

```json
// Request
{
  "titulo": "El Aleph",
  "autor": "Jorge Luis Borges",
  "isbn": "978-8420633109",
  "ubicacion": "A1-4",
  "stock": 4
}

// Response 201 Created
{
  "id": 4,
  "titulo": "El Aleph",
  "autor": "Jorge Luis Borges",
  "isbn": "978-8420633109",
  "ubicacion": "A1-4",
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
  "dni": "35555111"
}

// Response 201 Created
{
  "id": 3,
  "nombre": "Facundo",
  "apellido": "López",
  "email": "facundo.lopez@example.com",
  "dni": "35555111",
  "suspendidoHasta": null
}
```

### Préstamos

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| POST | `/api/prestamos` | Crear préstamo | 201 | 403, 404, 409 |
| GET | `/api/prestamos/{id}` | Obtener un préstamo | 200 | 404 |
| PUT | `/api/prestamos/{id}/devolucion` | Registrar devolución | 200 | 404, 409 |
| PUT | `/api/prestamos/{id}/extravio` | Marcar el préstamo como extraviado (bloquea al lector) | 200 | 404, 409 |
| PUT | `/api/prestamos/{id}/reposicion` | Registrar la reposición del ejemplar extraviado | 200 | 404, 409 |
| PUT | `/api/prestamos/{id}/renovacion` | Renovar 14 días más (máximo 2 veces) | 200 | 403, 404, 409 |
| GET | `/api/prestamos/mora` | Listar préstamos vencidos | 200 | — |
| GET | `/api/prestamos/activos` | Listar préstamos activos, por vencimiento | 200 | — |
| GET | `/api/prestamos/estado-lector/{lectorId}` | Si el lector puede pedir un préstamo y, si no, el motivo | 200 | 404 |

Ejemplo `POST /api/prestamos`:

```json
// Request
{
  "lectorId": 3,
  "libroId": 4
}

// Response 201 Created
{
  "id": 2,
  "lectorId": 3,
  "lectorNombreCompleto": "Facundo López",
  "libroId": 4,
  "libroTitulo": "El Aleph",
  "fechaPrestamo": "2026-09-18T01:00:00Z",
  "fechaVencimiento": "2026-10-02T01:00:00Z",
  "fechaDevolucion": null,
  "renovaciones": 0,
  "estado": "Activo",
  "enMora": false
}
```

### Reservas

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/reservas?libroId={id}` | Cola de reservas activas de un libro, en orden | 200 | 404 |
| GET | `/api/reservas/{id}` | Obtener una reserva | 200 | 404 |
| POST | `/api/reservas` | Reservar en cola un libro sin stock | 201 | 404, 409 |
| PUT | `/api/reservas/{id}/cancelacion` | Cancelar una reserva EnCola o Asignada | 200 | 404, 409 |

Ejemplo `POST /api/reservas`:

```json
// Request
{
  "lectorId": 2,
  "libroId": 3
}

// Response 201 Created
{
  "id": 1,
  "lectorId": 2,
  "lectorNombreCompleto": "Bruno Díaz",
  "libroId": 3,
  "libroTitulo": "Rayuela",
  "fechaReserva": "2026-11-06T19:06:11Z",
  "estado": "EnCola",
  "posicionEnCola": 1,
  "fechaAsignacion": null,
  "fechaLimiteRetiro": null
}
```

Cuando se devuelve (o se repone) un ejemplar de un libro con reservas, no vuelve al
stock: queda **Asignado** al primero de la cola con `fechaLimiteRetiro` = devolución +
24 h. Solo ese lector puede retirarlo con `POST /api/prestamos` (la reserva pasa a
`Concretada`). Si vence el plazo, la reserva queda `Vencida` y el ejemplar pasa al
siguiente o vuelve al stock.

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
  "id": 3,
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
ejecutar `Auth/Login Administrador (200)`, que guarda `tokenAdmin`, y
`Auth/Login Bibliotecario (200)`, que guarda `tokenBibliotecario`. La autenticación
se configura una sola vez en la colección (todos los requests heredan `tokenAdmin`,
salvo el 403 de bibliotecario). Los requests de creación generan email, DNI e ISBN
únicos en cada corrida, así la colección se puede ejecutar varias veces seguidas.

## Testing

- `tests/BiblioGest.UnitTests`: xUnit + Moq sobre los services, sin tocar
  la base real (175 tests).
- `tests/BiblioGest.IntegrationTests`: `WebApplicationFactory` levantando la
  API completa contra una SQLite en memoria aislada por instancia (95 tests).

Link: https://docs.google.com/document/d/1f4idud-PCBsQK\_48ZCf5ibF\_MNzNLKdxSAvWgO32Wt4/edit?tab=t.0#heading=h.frnk5y8ou0gl
