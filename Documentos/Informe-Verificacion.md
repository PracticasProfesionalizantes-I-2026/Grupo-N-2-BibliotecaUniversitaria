# Informe de verificación — rama `fix/validaciones`

Auditoría ejecutada el 2026-10-23 sobre `fix/validaciones` (local, con
`origin/fix/validaciones` como upstream). Cada verificación de este
informe se hizo ejecutando algo de verdad (build, test, curl, consulta
SQL o `bru run`) y mirando el resultado — no se marcó "Cumple" por haber
leído el código.

**Segunda ronda (misma fecha):** sobre los hallazgos de la primera
corrida, se corrigieron los puntos 1, 2, 3, 4 y 6 del informe original
(mensajes de `ProblemDetails`/401/403 en inglés o vacíos, Bruno no
idempotente y con dependencia cruzada de carpetas, y los huecos de
cobertura de tests), cada uno en su commit, y se volvió a correr todo
desde cero. Los resultados de abajo ya reflejan el estado final.

## Resumen

| Métrica | Resultado |
| --- | --- |
| `dotnet build` | **0 advertencias, 0 errores** (`dotnet clean` + build completo) |
| `dotnet test` | **181/181 pasan** (96 unitarios + 85 integración), 0 fallan, 0 omitidos |
| Bruno — corrida 1 (base limpia) | **58/58 asserts pasan** |
| Bruno — corrida 2 (sin reset) | **58/58 asserts pasan** |
| Bruno — corrida 3 (sin reset, extra) | **58/58 asserts pasan** |
| Requisitos verificados | 45 |
| → Cumple | 44 |
| → No cumple | 1 (limitación de SQLite, no un bug; ver abajo) |
| Correcciones aplicadas (1ª ronda) | 4 |
| Correcciones aplicadas (2ª ronda) | 5 + 1 doc de seguimiento |
| Pendiente sin corregir | 1 (`HasMaxLength` no se traduce al esquema de SQLite) |
| Casos que devolvieron 500 | **0** |

Commits de la 1ª ronda: `8814eb3`, `360d19d`, `70a1445`, `e67e686`.
Commits de la 2ª ronda: `d0970bf`, `9fa433f`, `3a458c2`, `22def45`,
`35538b5`, `67e9739`. No se tocó ningún archivo de test para que pasara
— donde hacía falta ajustar un test (ver punto 6), fue para agregar
cobertura nueva o para aislarlo en su propia fábrica cuando interfería
con otro test, nunca para esconder un fallo.

## Tabla de verificación (PASOS 3 a 6)

### PASO 3 — Bruno

| Requisito | Cómo se verificó | Esperado | Obtenido | Estado |
| --- | --- | --- | --- | --- |
| Login corre una sola vez, el resto reutiliza el token (`auth: inherit`) | `bru run --env Local`, inspección de `collection.bru` y de cada `.bru` | Un solo login por rol, resto hereda | Confirmado: 2 logins en toda la colección, el resto con `auth: inherit` (salvo el 403, a propósito) | ✅ |
| `tokenAdmin`/`tokenBibliotecario` separados, ninguno pisa al otro | Se leyó `bruno/environments/Local.bru` después de correr la colección | Dos tokens JWT distintos y válidos | Confirmado: dos JWT distintos, decodificados, con `role` Administrador y Bibliotecario respectivamente | ✅ |
| Cada request tiene `assert` de código HTTP | Inspección de los 58 `.bru` + salida de `bru run` | 58/58 con `assert` | 58/58 tienen `assert { res.status: ... }` | ✅ |
| Todos los asserts pasan (colección completa, base limpia) | `bru run --env Local` sobre `bibliogest.db` recién migrada y sembrada | 58/58 | **58/58** (antes: 46/54 — ver corrección "4. Bruno: autosuficiencia + orden de carpetas") | ✅ |
| El orden de ejecución del README funciona tal cual | Se corrió la colección completa de punta a punta sin intervención manual | Debía pasar de punta a punta | Pasa de punta a punta (antes dependía de reordenar carpetas a mano). El README ya no promete un orden imposible ni documenta limitaciones que ya no existen | ✅ |
| Segunda (y tercera) corrida no falla por datos de la primera | `bru run` tres veces seguidas sin resetear la base | 58/58 las 3 veces | **58/58 las 3 veces** (antes: 46/54, luego 41/54 en la segunda) | ✅ |

### PASO 4 — Pruebas manuales (curl)

| Requisito | Cómo se verificó | Esperado | Obtenido | Estado |
| --- | --- | --- | --- | --- |
| (a) Sin token / token mal formado / firma alterada / vencido → 401 | curl directo a `/api/libros` con cada variante (el vencido generado a mano con la misma `Jwt:Key`) | 401 en los 4 casos | 401 en los 4 casos | ✅ |
| (a) Bibliotecario en `/api/usuarios` → 403 | curl con `tokenBibliotecario` | 403 | 403 | ✅ |
| (a) Usuario desactivado intentando loguearse → 401 con mensaje | Se creó un usuario, se desactivó, se probó el login | 401, mensaje claro | 401, `{"message":"Email o contraseña inválidos."}` | ✅ |
| (a)/(g) Esos 401/403 tienen mensaje claro en español | Se inspeccionó el body de cada respuesta anterior tras configurar `JwtBearerEvents` | `{"message": "..."}` | `{"message":"Debe iniciar sesión."}` (sin token), `{"message":"Token inválido o vencido."}` (malformado/firma/vencido), `{"message":"No tiene permisos para realizar esta acción."}` (403); confirmado sin doble escritura del body (`HandleResponse()`) | ✅ |
| (b) El JWT trae `NameIdentifier`, email, role, exp, iss, aud y nada sensible | Se decodificó el payload a mano (como jwt.io) | Esos 6 claims, sin hash de password | Los 6 claims presentes (como URI largas de `ClaimTypes`, no nombres cortos), sin ningún dato de contraseña | ✅ |
| (b) El README tiene guía para jwt.io y es correcta | Se comparó el README contra el token real decodificado | Guía presente y precisa | Corregida en la 1ª ronda para mostrar las URI largas reales | ✅ |
| (c) Campo vacío/solo espacios/formato inválido/largo excedido/stock negativo/Guid vacío/fechas invertidas/tipo de reporte inexistente → 400 con mensaje claro | curl a cada endpoint con cada variante | 400, `{"message": "..."}` en español | 400 en todos los casos, mensaje en español y claro | ✅ |
| (c)/(g) Campo `null` explícito, body vacío, JSON mal formado, email de login inválido → 400 con mensaje claro | curl con body `null`, body vacío, JSON roto, y `POST /api/auth/login` con email sin `@`, tras configurar `InvalidModelStateResponseFactory` | 400, `{"message": "..."}` en español | `{"message":"El campo Titulo es obligatorio."}`, `{"message":"El cuerpo de la solicitud es obligatorio y no puede estar vacío."}`, `{"message":"El cuerpo de la solicitud no es un JSON válido."}`, `{"message":"El campo Email no tiene un formato de email válido."}` | ✅ |
| (d) ISBN repetido (con guiones distintos), identificador repetido (con espacios), email de lector y de usuario repetido (mayúsculas/espacios) → 409 | curl: crear, luego repetir con variante de formato | 409, mensaje claro | 409 en los 4 casos, mensaje claro en español | ✅ |
| (d) Segundo préstamo activo del mismo lector+libro → 409 | curl: crear préstamo, repetir mismo lector+libro | 409 | 409, `"...ya tiene un préstamo activo del libro..."` | ✅ |
| (d) Update que pone un valor ya usado por otro registro (ISBN de libro, email de lector) → 409 | curl: crear 2 libros/lectores, hacer `PUT` del segundo con el dato del primero; ahora también cubierto por tests unitarios nuevos | 409 | 409 en ambos casos | ✅ |
| (e) Más de 3 préstamos activos → 409 en el 4° | curl: 3 préstamos exitosos, 4° rechazado | 409 en el 4° | 409, y el libro del intento rechazado **no perdió stock** (confirmado en PASO 5) | ✅ |
| (e) Préstamo sin stock / lector en mora / devolución repetida → 409 | curl contra un libro en 0 stock, la lectora "Ana Gómez" del seed (mora) y una devolución duplicada | 409 en los 3 | 409 en los 3, mensajes claros | ✅ |
| (e) Borrar libro/lector con préstamo **activo** → 409 | curl `DELETE` con préstamo activo | 409 | 409 | ✅ |
| (e) Borrar libro/lector con préstamo **ya devuelto** → 409, NO 500 | Se creó libro+lector+préstamo, se devolvió, se intentó borrar ambos | 409, no 500 | **409 en los dos, confirmado que ya no es 500** (el bug original) | ✅ |
| (e) Admin se da de baja o se desactiva a sí mismo → 409 | curl `DELETE`/`PUT activo:false` sobre el propio admin logueado | 409 | 409 en ambos, mensaje claro | ✅ |
| (e) Desactivar/cambiar rol al último admin activo → 409 (caso propio y caso de otro caller) | Caso propio: único admin cambia su rol. Caso ajeno: se creó un 2° admin, se lo desactivó, y con **su token ya emitido** se intentó desactivar al único admin activo restante; ahora con test de integración propio (`UsuarioAutoBajaTests`) | 409 en ambos | 409 en ambos | ✅ |
| (f) Préstamo al día trae `diasRestantes`/`diasDeAtraso`/`estado`/`mensaje` correctos | curl a un préstamo recién creado | 14 restantes, 0 atraso, "Activo", "Vence en 14 días." | Exactamente eso | ✅ |
| (f) Préstamo vencido (el del seed, Rayuela/Ana Gómez) | `GET /api/prestamos/mora` | "Vencido", días de atraso > 0 | `estado:"Vencido"`, `diasDeAtraso:6`, `mensaje:"Atrasado 6 días."` | ✅ |
| (f) Préstamo devuelto a tiempo | curl: crear y devolver antes del vencimiento | "Devuelto", `diasRestantes: null` | Exactamente eso, `mensaje:"Devuelto a tiempo."` | ✅ |
| (f) Préstamo devuelto con atraso | Se adelantó `FechaVencimiento` 5 días vía SQL directo y se registró la devolución; ahora también con test de integración (`RegisterDevolucion_WhenOverdue_ReturnsDevueltoConAtraso`) | "Devuelto con atraso", `diasDeAtraso: 5` | Exactamente eso, `mensaje:"Devuelto con 5 días de atraso."` | ✅ |
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
| Largos máximos existen en el esquema | `.schema` completo | Alguna restricción de longitud visible | **No hay ninguna** — SQLite no soporta `VARCHAR(n)` para columnas `TEXT`; `HasMaxLength` de EF Core es metadata que no se traduce a una restricción real en SQLite. El límite solo existe en el código C# (verificado en PASO 4, y ahora con test unitario para Libro/Usuario) | ❌ (limitación de SQLite, ver sección "Pendiente") |

### PASO 6 — Trazabilidad

| Requisito | Cómo se verificó | Esperado | Obtenido | Estado |
| --- | --- | --- | --- | --- |
| Cada endpoint/método/test/request de Bruno que menciona la matriz existe de verdad | Se extrajeron por regex los métodos de service, nombres de test y requests de Bruno citados en `Trazabilidad.md`, y se buscó cada uno en el código/`tests/`/`bruno/` | 100% encontrados | 100% encontrados (0 faltantes) | ✅ |
| Cada identificador citado en los 8 CU (no solo en la matriz) existe de verdad | Mismo chequeo sobre los 8 `.md` de `Documentos/Casos de Uso/` (130 identificadores únicos) | 100% encontrados | 100% encontrados (0 faltantes) | ✅ |
| Todas las RN-xx de los CU están en la matriz | Se compararon los `RN-\d+` de los 8 `.md` contra los de `Trazabilidad.md` sección 1 | RN-01 a RN-21 en ambos lados | Coinciden exactamente, RN-01 a RN-21 | ✅ |
| El PDF (`BiblioGest_CasosDeUso.pdf`) no tiene reglas fuera de la matriz | Se leyeron las páginas accesibles del PDF (1-6; el renderizador de páginas adicionales no está instalado en este entorno — falta `poppler-utils`) | RN citadas en el PDF ⊆ matriz | RN-01 a RN-16 visibles en el PDF, texto idéntico al de los `.md`, todas ya en la matriz | ✅ (con la limitación de lectura del PDF anotada) |
| Los CU están actualizados con los cambios de este trabajo (ej. borrado con historial) | Se buscó "historial" en `cu-02` y `cu-04` | Debía reflejar la regla endurecida | Ambos documentan el cambio, con el nombre de excepción correcto y los tests reales referenciados | ✅ |
| El estado de cada fila de la matriz coincide con lo verificado | Se releyó la matriz fila por fila contra la evidencia de este informe (1ª y 2ª ronda) | Coincidencia | Coincide en todas las filas; "último administrador, otro caller" pasó de "Parcial" a "Cumple" (ahora con test de integración propio, no solo verificación manual) | ✅ |

## Correcciones aplicadas

### 1ª ronda (hallazgos del primer pase de verificación)

| # | Commit | Qué arregla |
| --- | --- | --- |
| 1 | `8814eb3` | "Obtener X por Id" corría antes que "Crear X" en Lectores y Libros (bug pre-existente en `develop`). |
| 2 | `360d19d` | El README prometía un orden de ejecución de Bruno imposible (circular). |
| 3 | `70a1445` | La guía de jwt.io del README decía nombres de claims que no son los reales. |
| 4 | `e67e686` | Estado de una fila de `Trazabilidad.md` corregido tras verificar con curl que la regla sí funciona. |

### 2ª ronda (a pedido explícito, puntos 1, 2, 3, 4 y 6 del informe original)

| # | Commit | Qué arregla | Re-verificación |
| --- | --- | --- | --- |
| 1 | `d0970bf` | `InvalidModelStateResponseFactory` en `Program.cs`: los 400 de model binding (campo `null`, body vacío, JSON inválido, email de login inválido) ahora devuelven `{"message": "..."}` en español, no el `ProblemDetails` en inglés por defecto. | curl a los 4 casos: mensajes en español confirmados (ver tabla PASO 4). |
| 2 | `9fa433f` | `JwtBearerEvents.OnChallenge`/`OnForbidden` en `Program.cs`: los 401/403 que resuelve el middleware de JWT (sin token, malformado, firma alterada, vencido, rol insuficiente) ahora devuelven `{"message": "..."}` en español, con `HandleResponse()` para no escribir el body dos veces. | curl a los 5 casos + verificación de `Content-Length`/headers sin doble escritura. |
| 3 | `3a458c2` | Los `Crear lector/libro/usuario (201)` de Bruno generan identificador/ISBN/email únicos por corrida (`Date.now()`, ISBN-13 con dígito verificador calculado en JS) en vez de datos fijos. | `bru run` dos veces seguidas sobre la misma base. |
| 4 | `22def45` | `Libros/Eliminar libro con prestamos activos (409)` pasa a ser autosuficiente (crea su propio lector+libro+préstamo en 3 requests previos de la misma carpeta) en vez de depender de la carpeta `Prestamos` (que corre después alfabéticamente) y de borrar el `libroId` compartido. De paso, `Prestamos/Crear prestamo sin stock (409)` pasa a usar el libro "Rayuela" del seed en vez de un `libroId` cuyo stock ya no quedaba en 0. | `bru run --env Local` tres veces seguidas sobre una base recién creada: **58/58 las 3 veces**. |
| 5 | `35538b5` | Tests nuevos para los huecos listados: solo-espacios (Libro, Usuario), update-duplicado (ISBN de libro, email de lector), body vacío/JSON inválido/campo null con verificación de mensaje en español, 401/403 con verificación de mensaje en español, token genéricamente malformado, "devuelto con atraso" de integración, "usuario inactivo hace login" de integración, y "último administrador, otro caller" de integración (aislado en `UsuarioAutoBajaTests`, no en `UsuariosControllerTests`, porque otro test de esa clase ya crea administradores adicionales y rompía el conteo de "último admin" para los tests que corren después). | `dotnet test`: 96 unitarios + 85 integración, 0 fallos. |
| — | `67e9739` | Seguimiento de documentación: el README ya no describe las 2 "limitaciones conocidas" de Bruno como si siguieran existiendo (las arregló la corrección #4), y actualiza los conteos de tests. | No aplica (solo texto). |

## Pendiente (no corregido — requiere una decisión de diseño)

Un solo punto sigue abierto, y no es un bug sino una limitación de la
base de datos elegida:

1. **`HasMaxLength` no se traduce a ninguna restricción real en el
   esquema SQLite** (confirmado con `.schema`): el límite de largo solo
   lo aplica el código C#. SQLite no tiene `VARCHAR(n)` real, así que EF
   Core no genera ningún `CHECK` para esto. **Propuesta:** si en algún
   momento se migra a SQL Server/Postgres, `HasMaxLength` ya está
   correctamente declarado en `BiblioGestDbContext` y se va a traducir
   solo al cambiar el provider — no hace falta tocar nada ahora. Si se
   insiste en reforzarlo en SQLite, se podrían agregar
   `CHECK (length(Campo) <= N)` a mano en una migración, pero es inusual
   y cambiaría el enfoque ya elegido (enforcement a nivel de aplicación).

Quedan, además, dos mejoras de testing que no pedía explícitamente el
punto 6 pero vale la pena anotar para el futuro:

- No hay un test que fuerce una falla a mitad de una transacción (por
  ejemplo, mockeando que `UpdateAsync` del libro lance una excepción
  después de crear el préstamo) para comprobar el rollback de
  `IUnitOfWork`. La atomicidad se verifica indirectamente (el stock
  siempre queda consistente tras una operación exitosa), pero no hay
  evidencia directa del camino de rollback.
- "Préstamo" no tiene campos de texto propios, así que el caso "solo
  espacios" de la lista original no aplica (`LectorId`/`LibroId` son
  `Guid`, ya cubiertos por el caso de Guid vacío).

## Nota sobre preparación (PASO 1)

`fix/validaciones` es una rama **local**, sin PR — `git pull` no trajo
nada porque no hay nada nuevo en `origin/fix/validaciones` más que lo que
ya se había subido. `bibliogest.db` se borró y se recreó corriendo la API
(que aplica las migraciones y siembra los datos de `DbInitializer`). De
paso: `dotnet ef database update` corrido desde `DataAccess/` crea la base
en esa carpeta, no en `API/` (la cadena de conexión del *design-time
factory* es relativa al directorio de trabajo) — no es un bug del código,
pero vale la pena saberlo si alguien más lo intenta a mano.
