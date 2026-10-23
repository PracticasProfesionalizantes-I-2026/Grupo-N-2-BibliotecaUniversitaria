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
  Middleware/         ExceptionMiddleware (mapea ValidationException→400, NotFoundException→404, ConflictException→409, UnauthorizedException→401; cualquier otra excepción → 500)
  Program.cs          DI, DbContext, DbInitializer, Swagger UI (OpenAPI)
BusinessLogic/      → BiblioGest.BusinessLogic
  Interfaces/         IAuthService, ILibroService, ILectorService, IPrestamoService, IReporteService, IUsuarioService, IPasswordHasher
  Services/           AuthService, LibroService, LectorService, PrestamoService, ReporteService, UsuarioService, PasswordHasher
  Validation/         EmailValidator, IsbnValidator (formato + normalización)
  Mapping/            PrestamoMapper (estado/días/mensaje calculados a partir de un TimeProvider)
DataAccess/         → BiblioGest.DataAccess (EF Core + SQLite)
  Entities/           Libro, Lector, Prestamo, EstadoPrestamo, Usuario, RolUsuario
  Repositories/       ILibroRepository/LibroRepository, etc.
  BiblioGestDbContext.cs    índices únicos + HasMaxLength
  IUnitOfWork/UnitOfWork.cs transacciones para préstamo + stock
  DbInitializer.cs    Crea la base y carga datos de prueba
  Migrations/
Shared/             → BiblioGest.Shared
  DTOs/               Create/Update/Response por entidad
  Exceptions/         NotFoundException, ValidationException, ConflictException, UnauthorizedException + concretas
bruno/              → Colección de requests (Auth, Libros, Lectores, Prestamos, Reportes, Usuarios)
tests/
  BiblioGest.UnitTests/        xUnit + Moq (services, sin tocar la base)
  BiblioGest.IntegrationTests/ WebApplicationFactory (API completa, SQLite en memoria)
```

**Responsabilidades por capa:**

| Capa | Responsabilidad |
| --- | --- |
| API | Recibe HTTP, valida el DTO de entrada, delega en el service. No mapea excepciones: eso lo centraliza `ExceptionMiddleware` |
| BusinessLogic | Reglas de negocio, orquestación, validaciones, normalización de datos, cálculos de fechas/estados/mensajes (vía `PrestamoMapper` + `TimeProvider`), `MapToResponseDTO` |
| DataAccess | Único acceso a la base vía EF Core (`AsNoTracking()` en lecturas, Guid asignado en `CreateAsync`, índices únicos + `HasMaxLength` en el `DbContext`) |
| Shared | DTOs y excepciones tipadas, compartidos entre capas |

Los services reciben sus repositorios (interfaces de `DataAccess`) por
constructor — nunca los instancian directamente — lo que permite mockearlos
con Moq en los tests unitarios.

## Entidades

- **Libro**: Id (Guid), Titulo (≤200), Autor (≤150), Isbn (ISBN-10/13 válido,
  único, se guarda sin guiones/espacios), Ubicacion (≤50), Stock (int, ≥ 0).
- **Lector**: Id (Guid), Nombre (≤100), Apellido (≤100), Email (formato
  válido, único, se guarda en minúsculas), Identificador (≤20, único — DNI
  o legajo en un solo campo, se guarda en mayúsculas).
- **Prestamo**: Id (Guid), LectorId, LibroId, FechaPrestamo,
  FechaVencimiento (FechaPrestamo + 14 días), FechaDevolucion (nullable),
  Estado (Activo/Devuelto persistido). El estado que ve el frontend
  (`Activo`/`Vencido`/`Devuelto`/`Devuelto con atraso`), los días
  restantes/de atraso y el mensaje se calculan en `PrestamoMapper` a partir
  de un único `TimeProvider`, nunca en el cliente.
- **Usuario**: Id (Guid), Email (formato válido, ≤150, único, se guarda en
  minúsculas), PasswordHash (PBKDF2), RolUsuario (Administrador/
  Bibliotecario), Activo (baja lógica).

`Prestamo` es hijo de `Libro` y `Lector` con `DeleteBehavior.Restrict` a
nivel de base de datos. Además, el service rechaza con 409 el borrado de
un libro o lector que tenga **cualquier** préstamo en su historial (activo
o ya devuelto): el historial nunca se borra. Un lector tampoco puede tener
dos préstamos activos del mismo libro a la vez (índice único filtrado por
`Estado = 'Activo'` + validación en el service).

## Reglas de negocio y excepciones

| Regla | Excepción (`Shared/Exceptions/`) | HTTP |
| --- | --- | --- |
| Datos obligatorios del libro faltantes, texto demasiado largo o stock negativo | `LibroInvalidoException` | 400 |
| ISBN con formato inválido (no es ISBN-10/13 con dígito verificador correcto) | `LibroInvalidoException` | 400 |
| ISBN duplicado (comparando el valor normalizado, sin guiones/espacios) | `IsbnDuplicadoException` | 409 |
| Datos obligatorios del lector faltantes, texto demasiado largo o email inválido | `LectorInvalidoException` | 400 |
| Email de lector duplicado (sin distinguir mayúsculas/espacios) | `LectorEmailDuplicadoException` | 409 |
| Identificador (DNI/legajo) duplicado (normalizado) | `IdentificadorDuplicadoException` | 409 |
| Libro/Lector/Préstamo inexistente | `LibroNotFoundException` / `LectorNotFoundException` / `PrestamoNotFoundException` | 404 |
| `LectorId`/`LibroId` vacíos (`Guid.Empty`) al crear un préstamo | `PrestamoInvalidoException` | 400 |
| Eliminar libro/lector con préstamos en su historial (activos o ya devueltos) | `LibroConHistorialPrestamosException` / `LectorConHistorialPrestamosException` | 409 |
| Lector con 3 préstamos activos | `LimitePrestamosActivosException` | 409 |
| Lector con un préstamo activo del mismo libro | `PrestamoActivoDuplicadoException` | 409 |
| Libro sin stock disponible | `StockInsuficienteException` | 409 |
| Lector con préstamos vencidos (mora) | `LectorEnMoraException` | 409 |
| Devolver un préstamo ya devuelto | `PrestamoYaDevueltoException` | 409 |
| Credenciales inválidas o usuario inactivo (login) | `CredencialesInvalidasException` | 401 |
| Usuario sin email/email inválido, contraseña < 6 caracteres o rol inexistente (RN-20, RN-21) | `UsuarioInvalidoException` | 400 |
| Usuario inexistente | `UsuarioNotFoundException` | 404 |
| Email de usuario duplicado (sin distinguir mayúsculas/espacios) | `UsuarioDuplicadoException` | 409 |
| Un administrador se da de baja o se desactiva a sí mismo | `UsuarioAutoBajaException` | 409 |
| Desactivar o cambiar el rol del último administrador activo | `UltimoAdministradorException` | 409 |
| Reporte inexistente o filtro de fechas inválido (`desde` > `hasta`) | `ReporteInvalidoException` | 400 |
| Usuario sin rol Administrador en `/api/usuarios` (RN-19) | `[Authorize(Roles)]` | 403 |

Todas heredan de una categoría base (`NotFoundException`,
`ValidationException`, `ConflictException`, `UnauthorizedException`).
Los controllers no tienen `try/catch`: `ExceptionMiddleware` centraliza el
mapeo de esas 4 categorías a 400/404/409/401. Cualquier otra excepción no
prevista también la captura el middleware y responde `500` con
`{ "message": "Ocurrió un error interno en el servidor." }`.

## Cómo ejecutar

Desde la raíz del repositorio:

```bash
dotnet build BiblioGest.slnx      # compila toda la solución
dotnet test BiblioGest.slnx       # corre unitarios (Moq) e integración (WebApplicationFactory)
dotnet run --project API          # levanta la API (crea bibliogest.db con datos de prueba)
```

> Si ya tenías una `bibliogest.db` de una corrida anterior, borrala antes de
> levantar la API: se agregaron índices únicos nuevos (ISBN, email de
> lector) y se corrigieron ISBN de prueba inválidos, y `Database.Migrate()`
> puede fallar si la base vieja tiene datos que ya violan esas reglas.

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
  "isbn": "978-8420633107",
  "ubicacion": "Estante A4",
  "stock": 4
}

// Response 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "titulo": "El Aleph",
  "autor": "Jorge Luis Borges",
  "isbn": "9788420633107",
  "ubicacion": "Estante A4",
  "stock": 4,
  "disponible": true,
  "disponibilidadTexto": "Disponible (4 ejemplares)"
}
```

El ISBN se guarda sin guiones ni espacios (se normaliza antes de
persistir), y se valida el dígito verificador de ISBN-10/13.

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
| POST | `/api/prestamos` | Crear préstamo | 201 | 400, 404, 409 |
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
  "enMora": false,
  "diasRestantes": 14,
  "diasDeAtraso": 0,
  "mensaje": "Vence en 14 días."
}
```

`estado` puede ser `Activo`, `Vencido`, `Devuelto` o `Devuelto con atraso`;
`diasRestantes` es `null` una vez devuelto. Todo lo calcula `PrestamoMapper`
en el service (nunca el frontend).

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

Bruno es la herramienta elegida para probar manualmente la API (no se usa
Postman ni ningún otro cliente). En `bruno/` hay una carpeta por controller
con un request por caso de éxito y de error relevante
(400/401/403/404/409).

**Autenticación a nivel de colección.** `collection.bru` define
`auth: bearer` usando `{{tokenAdmin}}`, y todos los requests protegidos
usan `auth: inherit` — no repiten el token en cada request. La excepción es
"Usuarios/Listar usuarios como bibliotecario (403)", que fija
explícitamente `{{tokenBibliotecario}}` porque necesita probar el rol
equivocado a propósito.

- `Auth/Login Administrador (200)` guarda `tokenAdmin` (el que usa la
  colección).
- `Auth/Login Bibliotecario (200)` guarda `tokenBibliotecario` por
  separado, sin pisar `tokenAdmin`.

El entorno `Local` trae `baseUrl` y los requests de creación completan
automáticamente `libroId`/`lectorId`/`prestamoId`/`usuarioId` para
encadenar los siguientes. Cada request tiene un bloque `assert` que verifica
el código HTTP esperado.

**Orden real de ejecución.** `bru run --env Local` (o el botón "Run" sobre
la colección completa) recorre las carpetas en orden alfabético: **Auth →
Lectores → Libros → Prestamos → Reportes → Usuarios**, y dentro de cada
carpeta respeta el campo `seq` de cada request. `Auth/Login Administrador
(200)` corre primero y carga `tokenAdmin`; a partir de ahí todo lo
protegido reutiliza ese token vía `auth: inherit`.

**Limitación conocida (verificada corriendo la colección con el CLI de
Bruno):** `Libros/Eliminar libro con prestamos activos (409)` necesita que
el libro tenga un préstamo asociado, pero la carpeta `Prestamos` corre
*después* de `Libros` en el orden alfabético — así que en una corrida
completa de punta a punta ese request encuentra el libro sin préstamos
todavía y lo borra con `200` en vez de `409` (y esa borrada en cascada hace
fallar, de paso, "Crear libro con isbn duplicado" y todos los requests de
`Prestamos` que dependían de ese libro). Para verificar manualmente ese
caso puntual hay que correr primero `Prestamos/Crear prestamo (201)` con
ese `libroId` y recién después el `DELETE`, o ejecutarlo como test de
integración (`DeleteLibro_WithActiveLoans_Returns409Conflict`, que sí crea
su propio lector/libro/préstamo y no depende del orden de carpetas).

**Otra limitación conocida:** los requests "Crear lector/libro/usuario
(201)" usan datos fijos (identificador, ISBN, email). Correr la colección
completa dos veces seguidas sin reiniciar la base hace que esos `POST`
choquen con los registros que quedaron de la corrida anterior y devuelvan
`409` en vez de `201` (y alguno de los "duplicado (409)" pase a dar `201`,
porque el registro original fue modificado por `Modificar ... (200)` en la
corrida previa). Para una corrida limpia, borrar `bibliogest.db` antes de
cada ejecución completa de la colección.

## Guía rápida: inspeccionar el JWT en jwt.io

1. Copiar el `token` de cualquier respuesta de `POST /api/auth/login` (o el
   valor de `tokenAdmin`/`tokenBibliotecario` en el entorno de Bruno).
2. Pegarlo en https://jwt.io en el panel **Encoded**.
3. En el panel decodificado, el **payload** debe mostrar exactamente estos
   claims (y ningún dato sensible como el hash de la contraseña):
   - `nameid` (`ClaimTypes.NameIdentifier`): el Id del usuario.
   - `email`: el email normalizado (minúsculas).
   - `role`: `Administrador` o `Bibliotecario`.
   - `exp`: fecha de expiración (60 minutos después del login).
   - `iss`: `BiblioGestApi`.
   - `aud`: `BiblioGestClient`.
4. Para comprobar que un token modificado a mano se rechaza: en el panel
   **Encoded**, cambiar un solo carácter de la firma (la tercera parte,
   después del segundo `.`) o del payload, y usar ese token en un request
   protegido (por ejemplo `Libros/Listar libros con token invalido (401)`
   en Bruno, o `GET /api/libros` con ese header `Authorization: Bearer
   <token modificado>`). La API debe responder `401 Unauthorized`, porque
   la firma ya no coincide con la clave configurada en `Jwt:Key`
   (`appsettings.json`).

## Testing

- `tests/BiblioGest.UnitTests`: xUnit + Moq sobre los services, sin tocar
  la base real (91 tests).
- `tests/BiblioGest.IntegrationTests`: `WebApplicationFactory` levantando la
  API completa contra una SQLite en memoria aislada por instancia (74 tests).

Link: https://docs.google.com/document/d/1f4idud-PCBsQK\_48ZCf5ibF\_MNzNLKdxSAvWgO32Wt4/edit?tab=t.0#heading=h.frnk5y8ou0gl
