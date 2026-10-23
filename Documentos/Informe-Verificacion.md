# Informe de verificación — rama `fix/validaciones`

Auditoría ejecutada el 2026-10-23 sobre `fix/validaciones` (local, con
`origin/fix/validaciones` como upstream). Cada verificación de este
informe se hizo ejecutando algo de verdad (build, test, curl, consulta
SQL o `bru run`) y mirando el resultado — no se marcó "Cumple" por haber
leído el código.

## Resumen

| Métrica | Resultado |
| --- | --- |
| `dotnet build` | **0 advertencias, 0 errores** (PASO 1, build limpio con `dotnet clean` previo) |
| `dotnet test` | **165/165 pasan** (91 unitarios + 74 integración), 0 fallan, 0 omitidos |
| Bruno — corrida 1 (base limpia) | **46/54 asserts pasan**, 8 fallan |
| Bruno — corrida 2 (sin reset) | **41/54 asserts pasan**, 13 fallan |
| Requisitos verificados | 30 |
| → Cumple | 23 |
| → Parcial | 1 |
| → No cumple | 6 |
| Correcciones aplicadas | 4 (bugs/documentación acotados, cada uno en su commit) |
| Pendientes sin corregir | 6 (requieren decisión de diseño, quedan propuestos) |
| Casos que devolvieron 500 | **0** |

Los commits de corrección quedaron en `fix/validaciones`
(`8814eb3`, `360d19d`, `70a1445`, `e67e686`), además de este informe.
No se tocó ningún archivo de test para que pasara.

## Tabla de verificación (PASOS 3 a 6)

### PASO 3 — Bruno

| Requisito | Cómo se verificó | Esperado | Obtenido | Estado |
| --- | --- | --- | --- | --- |
| Login corre una sola vez, el resto reutiliza el token (`auth: inherit`) | `bru run --env Local`, inspección de `collection.bru` y de cada `.bru` | Un solo login por rol, resto hereda | Confirmado: 2 logins en toda la colección, 52 requests con `auth: inherit` (salvo el 403, a propósito) | ✅ |
| `tokenAdmin`/`tokenBibliotecario` separados, ninguno pisa al otro | Se leyó `bruno/environments/Local.bru` después de correr la colección | Dos tokens JWT distintos y válidos | Confirmado: dos JWT distintos, decodificados, con `role` Administrador y Bibliotecario respectivamente | ✅ |
| Cada request tiene `assert` de código HTTP | Inspección de los 54 `.bru` + salida de `bru run` | 54/54 con `assert` | 54/54 tienen `assert { res.status: ... }` | ✅ |
| Todos los asserts pasan (colección completa, base limpia) | `bru run --env Local` sobre `bibliogest.db` recién migrada y sembrada | 54/54 | **46/54** — 8 fallan, todos por una sola causa raíz (ver corrección #1 pendiente abajo) | ❌ |
| El orden de ejecución del README funciona tal cual | Se siguió el orden documentado y se corrió la colección completa | Debía pasar de punta a punta | El orden documentado era **circular e imposible** (paso 2 pedía algo creado en el paso 4). Corregido en el commit `360d19d`: ahora el README describe el orden real (alfabético por carpeta) y documenta la limitación en vez de prometer algo falso | ❌ (doc corregida, la limitación de fondo sigue) |
| Segunda corrida no falla por datos de la primera | `bru run` dos veces seguidas sin resetear la base | 54/54 ambas veces | 1ª: 46/54. 2ª (sin reset): **41/54** — 5 fallas nuevas por `POST` con identificador/ISBN/email fijos que chocan con lo creado antes | ❌ |

### PASO 4 — Pruebas manuales (curl)

| Requisito | Cómo se verificó | Esperado | Obtenido | Estado |
| --- | --- | --- | --- | --- |
| (a) Sin token / token mal formado / firma alterada / vencido → 401 | curl directo a `/api/libros` con cada variante (el vencido generado a mano con la misma `Jwt:Key`) | 401 en los 4 casos | 401 en los 4 casos | ✅ |
| (a) Bibliotecario en `/api/usuarios` → 403 | curl con `tokenBibliotecario` | 403 | 403 | ✅ |
| (a) Usuario desactivado intentando loguearse → 401 con mensaje | Se creó un usuario, se desactivó, se probó el login | 401, mensaje claro | 401, `{"message":"Email o contraseña inválidos."}` | ✅ |
| (a)/(g) Esos 401/403 tienen mensaje claro en español | Se inspeccionó el body de cada respuesta anterior | `{"message": "..."}` | **Body vacío** en los 5 casos de token (401/403 resueltos por el middleware de JWT de ASP.NET, no pasan por `ExceptionMiddleware`) | ❌ |
| (b) El JWT trae `NameIdentifier`, email, role, exp, iss, aud y nada sensible | Se decodificó el payload a mano (como jwt.io) | Esos 6 claims, sin hash de password | Los 6 claims presentes (como URI largas de `ClaimTypes`, no nombres cortos), sin ningún dato de contraseña | ✅ |
| (b) El README tiene guía para jwt.io y es correcta | Se comparó el README contra el token real decodificado | Guía presente y precisa | Estaba presente pero decía que los claims se ven como `nameid`/`email`/`role`; en realidad aparecen con la URI larga. Corregido en `70a1445` | ✅ (tras la corrección) |
| (c) Campo vacío/solo espacios/formato inválido/largo excedido/stock negativo/Guid vacío/fechas invertidas/tipo de reporte inexistente → 400 con mensaje claro | curl a cada endpoint con cada variante | 400, `{"message": "..."}` en español | 400 en todos los casos, mensaje en español y claro **cuando el request llega a nuestro código** | ✅ |
| (c)/(g) Campo `null` explícito, body vacío, JSON mal formado, email de login inválido → 400 con mensaje claro | curl con body `null`, body vacío, JSON roto, y `POST /api/auth/login` con email sin `@` | 400, `{"message": "..."}` en español | 400 sí, pero **en inglés** y con el formato `ProblemDetails` de ASP.NET (`"errors": {...}`), porque `[ApiController]` intercepta la validación antes de llegar al service | ❌ |
| (d) ISBN repetido (con guiones distintos), identificador repetido (con espacios), email de lector y de usuario repetido (mayúsculas/espacios) → 409 | curl: crear, luego repetir con variante de formato | 409, mensaje claro | 409 en los 4 casos, mensaje claro en español | ✅ |
| (d) Segundo préstamo activo del mismo lector+libro → 409 | curl: crear préstamo, repetir mismo lector+libro | 409 | 409, `"...ya tiene un préstamo activo del libro..."` | ✅ |
| (d) Update que pone un valor ya usado por otro registro (ISBN de libro, email de lector) → 409 | curl: crear 2 libros/lectores, hacer `PUT` del segundo con el dato del primero | 409 | 409 en ambos casos (este camino no tenía test automático, ver PASO 2) | ✅ |
| (e) Más de 3 préstamos activos → 409 en el 4° | curl: 3 préstamos exitosos, 4° rechazado | 409 en el 4° | 409, y el libro del intento rechazado **no perdió stock** (confirmado en PASO 5) | ✅ |
| (e) Préstamo sin stock / lector en mora / devolución repetida → 409 | curl contra un libro en 0 stock, la lectora "Ana Gómez" del seed (mora) y una devolución duplicada | 409 en los 3 | 409 en los 3, mensajes claros | ✅ |
| (e) Borrar libro/lector con préstamo **activo** → 409 | curl `DELETE` con préstamo activo | 409 | 409 | ✅ |
| (e) Borrar libro/lector con préstamo **ya devuelto** → 409, NO 500 | Se creó libro+lector+préstamo, se devolvió, se intentó borrar ambos | 409, no 500 | **409 en los dos, confirmado que ya no es 500** (el bug original) | ✅ |
| (e) Admin se da de baja o se desactiva a sí mismo → 409 | curl `DELETE`/`PUT activo:false` sobre el propio admin logueado | 409 | 409 en ambos, mensaje claro | ✅ |
| (e) Desactivar/cambiar rol al último admin activo → 409 (caso propio y caso de otro caller) | Caso propio: único admin cambia su rol. Caso ajeno: se creó un 2° admin, se lo desactivó, y con **su token ya emitido** se intentó desactivar al único admin activo restante | 409 en ambos | 409 en ambos, incluido el camino "otro caller" que antes solo tenía test unitario (mockeado) | ✅ |
| (f) Préstamo al día trae `diasRestantes`/`diasDeAtraso`/`estado`/`mensaje` correctos | curl a un préstamo recién creado | 14 restantes, 0 atraso, "Activo", "Vence en 14 días." | Exactamente eso | ✅ |
| (f) Préstamo vencido (el del seed, Rayuela/Ana Gómez) | `GET /api/prestamos/mora` | "Vencido", días de atraso > 0 | `estado:"Vencido"`, `diasDeAtraso:6`, `mensaje:"Atrasado 6 días."` | ✅ |
| (f) Préstamo devuelto a tiempo | curl: crear y devolver antes del vencimiento | "Devuelto", `diasRestantes: null` | Exactamente eso, `mensaje:"Devuelto a tiempo."` | ✅ |
| (f) Préstamo devuelto con atraso | Se adelantó `FechaVencimiento` 5 días vía SQL directo y se registró la devolución | "Devuelto con atraso", `diasDeAtraso: 5` | Exactamente eso, `mensaje:"Devuelto con 5 días de atraso."` | ✅ |
| (f) Libro trae texto de disponibilidad | curl a libros con distinto stock | "Disponible (N ejemplares)" / "Disponible (1 ejemplar)" / "Sin stock" | Los 3 textos correctos según el stock (1, N, 0) | ✅ |
| (g) Ningún caso probado devuelve 500 | Se revisaron todas las respuestas guardadas y el log de la API buscando "Error no controlado" | 0 ocurrencias | 0 ocurrencias | ✅ |

### PASO 5 — Integridad de `bibliogest.db`

| Requisito | Cómo se verificó | Esperado | Obtenido | Estado |
| --- | --- | --- | --- | --- |
| Sin strings vacíos/solo espacios/con espacios al borde | SQL: `WHERE campo='' OR trim(campo)='' OR campo!=trim(campo)` en Nombre, Apellido, Email, Identificador, Titulo, Autor, Isbn, Ubicacion | 0 filas | 0 filas | ✅ |
| Emails válidos, en minúsculas, sin duplicados | SQL: `Email != lower(Email)`, `NOT LIKE '%@%.%'`, `GROUP BY lower(trim(Email)) HAVING COUNT(*)>1` en Lectores y Usuarios | 0 filas en los 3 casos | 0 filas en los 3 casos | ✅ |
| ISBN válidos (10 o 13 caracteres) y sin duplicados | SQL: `length(Isbn) NOT IN (10,13)`, `GROUP BY Isbn HAVING COUNT(*)>1` | 0 filas | 0 filas | ✅ |
| Identificador de lector sin duplicados (normalizado) | SQL: `GROUP BY upper(trim(Identificador)) HAVING COUNT(*)>1` | 0 filas | 0 filas | ✅ |
| Stock no negativo | SQL: `WHERE Stock < 0` | 0 filas | 0 filas | ✅ |
| Sin préstamos huérfanos | SQL: `LEFT JOIN` de Prestamos contra Lectores/Libros buscando `NULL` | 0 filas | 0 filas | ✅ |
| Préstamos devueltos siempre con `FechaDevolucion`, activos nunca con `FechaDevolucion` | SQL: `Estado='Devuelto' AND FechaDevolucion IS NULL` / `Estado='Activo' AND FechaDevolucion IS NOT NULL` | 0 filas en ambos | 0 filas en ambos | ✅ |
| Stock de cada libro coherente con su historial de préstamos | Se tabuló Stock vs. préstamos activos/históricos por libro y se verificó a mano contra cada operación hecha en el PASO 4 (11 libros) | Cada libro con sentido según lo hecho | Los 11 libros coherentes (stock baja al prestar, sube al devolver, no se mueve si el préstamo fue rechazado) | ✅ |
| Índices únicos existen en el esquema | `.schema` completo | Índice único en Isbn, Identificador, Email (lector), Email (usuario), y el filtrado de préstamo activo | Los 5 índices únicos presentes y correctos, incluido el filtrado `WHERE "Estado" = 'Activo'` | ✅ |
| Largos máximos existen en el esquema | `.schema` completo | Alguna restricción de longitud visible | **No hay ninguna** — SQLite no soporta `VARCHAR(n)` para columnas `TEXT`; `HasMaxLength` de EF Core es metadata que no se traduce a una restricción real en SQLite. El límite solo existe en el código C# (ya verificado en PASO 4) | ❌ |

### PASO 6 — Trazabilidad

| Requisito | Cómo se verificó | Esperado | Obtenido | Estado |
| --- | --- | --- | --- | --- |
| Cada endpoint/método/test/request de Bruno que menciona la matriz existe de verdad | Se extrajeron por regex los 16 métodos de service, 47 nombres de test y 24 requests de Bruno citados en `Trazabilidad.md`, y se buscó cada uno en el código/`tests/`/`bruno/` | 100% encontrados | 100% encontrados (0 faltantes) | ✅ |
| Cada identificador citado en los 8 CU (no solo en la matriz) existe de verdad | Mismo chequeo sobre los 8 `.md` de `Documentos/Casos de Uso/` (130 identificadores únicos) | 100% encontrados | 100% encontrados (0 faltantes) | ✅ |
| Todas las RN-xx de los CU están en la matriz | Se compararon los `RN-\d+` de los 8 `.md` contra los de `Trazabilidad.md` sección 1 | RN-01 a RN-21 en ambos lados | Coinciden exactamente, RN-01 a RN-21 | ✅ |
| El PDF (`BiblioGest_CasosDeUso.pdf`) no tiene reglas fuera de la matriz | Se leyeron las páginas accesibles del PDF (1-6; el renderizador de páginas adicionales no está instalado en este entorno — falta `poppler-utils`) | RN citadas en el PDF ⊆ matriz | RN-01 a RN-16 visibles en el PDF, texto idéntico al de los `.md`, todas ya en la matriz. No se pudieron render izar las páginas siguientes del PDF para confirmar que no haya RN adicionales (RN-17 en adelante); esas sí están cubiertas vía los `.md`, que son la fuente usada para implementar | ✅ (con la limitación de lectura del PDF anotada) |
| Los CU están actualizados con los cambios de este trabajo (ej. borrado con historial) | Se buscó "historial" en `cu-02` y `cu-04` | Debía reflejar la regla endurecida | Ambos documentan el cambio, con el nombre de excepción correcto y los tests reales referenciados | ✅ |
| El estado de cada fila de la matriz coincide con lo verificado en los PASOS 3-5 | Se releyó la matriz fila por fila contra la evidencia de este informe | Coincidencia | Coincidía en todo salvo una fila ("último administrador, otro caller"), que decía "Parcial" y en este audit se confirmó que la regla sí funciona (solo falta el test automático). Corregido en `e67e686` | ✅ (tras la corrección) |

## Correcciones aplicadas

Cada una es acotada, está en su propio commit, y se volvió a correr la
verificación del punto después de aplicarla.

| # | Commit | Qué arregla | Re-verificación |
| --- | --- | --- | --- |
| 1 | `8814eb3` | `bruno/Lectores/Obtener lector por Id.bru` y `bruno/Libros/Obtener libro por Id.bru` corrían (por `seq`) **antes** que el `Crear` correspondiente, así que pedían un recurso que todavía no existía y daban 404 en vez de 200. Se intercambiaron los `seq` para que el alta corra primero. Bug pre-existente en `develop`, nunca detectado porque antes esos requests no tenían `assert`. | Se volvió a correr `bru run`: esos 2 requests pasan a `200 OK` (antes: 404). Confirmado en la corrida limpia (46/54) y en la segunda corrida (41/54) — en ambas, esos 2 ya no están entre los fallos. |
| 2 | `360d19d` | El README prometía un "orden de ejecución recomendado" imposible (el paso 2 de Libros decía depender de algo creado en el paso 4 de Prestamos, que corre después). Se reemplazó por la descripción real (orden alfabético por carpeta) y se documentaron las 2 limitaciones conocidas (dependencia cruzada de carpetas, y datos fijos que rompen la segunda corrida). | Es una corrección de documentación; se confirmó releyendo el texto contra el comportamiento real observado en `bru run`. |
| 3 | `70a1445` | La guía de jwt.io del README decía que los claims se ven como `nameid`/`email`/`role`; decodificando un token real aparecen con la URI larga de `ClaimTypes` (`.../identity/claims/nameidentifier`, etc.). | Se decodificó el token del admin con Python (como haría jwt.io) y se confirmó que el texto corregido coincide exactamente con lo que se ve. |
| 4 | `e67e686` | La fila "último administrador activo" de `Trazabilidad.md` decía "Parcial" porque el camino "lo hace un admin distinto" solo tenía test unitario (mockeado). Se verificó manualmente con curl que la regla funciona de punta a punta contra la API real (ver PASO 4), así que se corrigió el estado a "Cumple", aclarando que lo que falta es el test automático, no la funcionalidad. | Evidencia queda documentada en la tabla del PASO 4 de este mismo informe. |

## Pendiente (no corregido — requiere una decisión de diseño)

Cada uno de estos **no** se tocó porque cambia diseño o arquitectura, tal
como indican las reglas de esta auditoría. Se deja la propuesta concreta
para que se decida aparte.

1. **Mensajes de error en inglés cuando ASP.NET intercepta la validación
   antes que nuestro código** (campo `null` explícito, body vacío, JSON mal
   formado, y el email de login por tener `[EmailAddress]` como data
   annotation). Rompe el requisito "todos los mensajes de error en
   español". **Propuesta:** en `Program.cs`, configurar
   `builder.Services.AddControllers().ConfigureApiBehaviorOptions(options
   => options.InvalidModelStateResponseFactory = ...)` para devolver
   `{"message": "..."}` en español con el mismo formato que usa
   `ExceptionMiddleware`, en vez del `ProblemDetails` por defecto. Decisión
   de diseño pendiente: qué texto exacto mostrar cuando hay varios campos
   inválidos a la vez.
2. **401/403 con body vacío** cuando el rechazo lo resuelve el middleware
   de JWT de ASP.NET (sin token, token mal formado, firma alterada,
   vencido, o rol insuficiente) — nunca llegan a `ExceptionMiddleware`.
   **Propuesta:** configurar `JwtBearerEvents.OnChallenge` y
   `OnForbidden` en el `AddJwtBearer(...)` de `Program.cs` para escribir
   `{"message": "No autenticado." }` / `{"message": "No tenés permiso para
   esta acción."}` en vez de dejar el body vacío.
3. **`Libros/Eliminar libro con prestamos activos (409)` no puede pasar en
   una corrida completa de la colección** porque depende de un préstamo
   que se crea en la carpeta `Prestamos`, que corre después de `Libros`
   alfabéticamente. Además borra el libro de verdad cuando corre antes de
   tiempo, lo que hace fallar en cascada "Crear libro con isbn duplicado" y
   los 6 requests de `Prestamos` que reusan ese `libroId`.
   **Propuesta:** reescribir ese request para que cree su propio
   lector+libro+préstamo embebidos (como ya hace el test de integración
   `DeleteLibro_WithActiveLoans_Returns409Conflict`), en vez de depender
   de `{{libroId}}` compartido con otra carpeta.
4. **La colección de Bruno no es segura para correr dos veces seguidas**:
   los requests "Crear lector/libro/usuario (201)" usan datos fijos
   (identificador, ISBN, email), así que la segunda corrida choca con lo
   que quedó de la primera — y además, como "Modificar ... (200)" cambia
   el identificador/dato original, algunos "duplicado (409)" pasan a dar
   `201` en la segunda corrida (porque el valor original quedó libre).
   **Propuesta:** generar esos valores con una función aleatoria por
   corrida (por ejemplo, un script `pre-request` con `Math.random()` o
   `{{$randomUUID}}` recortado) en vez de strings fijos, igual que ya se
   hizo en los tests de integración (`IsbnTestHelper`, `Guid.NewGuid()`).
5. **`HasMaxLength` no se traduce a ninguna restricción real en el esquema
   SQLite** (confirmado con `.schema`): el límite de largo solo lo aplica
   el código C#. No es exactamente un bug — es una limitación de SQLite
   (no tiene `VARCHAR(n)` real) — pero como la consigna pedía confirmar
   que el largo máximo "existe en el esquema", queda marcado como no
   cumplido en sentido estricto. **Propuesta:** si se necesita
   enforcement a nivel de base (por ejemplo, de cara a una migración
   futura a SQL Server/Postgres donde si se traduce a `VARCHAR(n)`), no
   hace falta tocar nada — ya está declarado correctamente en
   `BiblioGestDbContext` y se traducirá solo con el provider correcto. Si
   se insiste en SQLite, se podrían agregar `CHECK (length(Campo) <= N)`
   a mano en la migración, pero es inusual y no se hizo porque cambia el
   enfoque ya elegido (enforcement a nivel de aplicación).
6. **Cobertura de tests automáticos** (no son bugs de la API, son huecos
   de test, ya señalados en el PASO 2 y en `Trazabilidad.md`):
   - Ningún test de "solo espacios" para Libro/Usuario/Préstamo (sólo
     Lector lo tiene).
   - Ningún test de campo omitido del JSON, body vacío o JSON inválido
     (quedaron cubiertos recién en este audit, manualmente).
   - Update-duplicado de ISBN (libro) y de email (lector) sin test
     automático (verificado manualmente en este audit, PASO 4-d).
   - "Devuelto con atraso" y "usuario inactivo intenta loguearse" solo
     tienen test unitario, no de integración.
   - Token genéricamente malformado (no vencido, no con firma alterada)
     sin test automático (solo el request de Bruno).
   - "Último administrador, otro caller" sin test de integración
     (verificado manualmente, ver corrección #4).
   **Propuesta:** agregar estos casos como tests de integración siguiendo
   el patrón ya usado en `TokenValidationTests`/`UsuarioAutoBajaTests`
   (fábrica propia o datos generados con `Guid.NewGuid()` para no
   interferir con otros tests).

## Nota sobre preparación (PASO 1)

`fix/validaciones` es una rama **local**, sin PR — `git pull` no trajo
nada porque no hay nada nuevo en `origin/fix/validaciones` más que lo que
ya se había subido. `bibliogest.db` se borró y se recreó corriendo la API
(que aplica las migraciones y siembra los datos de `DbInitializer`). De
paso: `dotnet ef database update` corrido desde `DataAccess/` crea la base
en esa carpeta, no en `API/` (la cadena de conexión del *design-time
factory* es relativa al directorio de trabajo) — no es un bug del código,
pero vale la pena saberlo si alguien más lo intenta a mano.
