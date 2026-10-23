# Matriz de trazabilidad — Requerimientos / Reglas de negocio ↔ Casos de uso ↔ Código

Generada en la rama `fix/validaciones`, a partir de los documentos en
`Documentos/Casos de Uso/` (cu-01 a cu-08) y del código real al momento de
este trabajo. "Estado" refleja lo que el código efectivamente hace, no lo
que el CU dice que debería hacer — las diferencias quedan en la sección
**(c)** más abajo.

Convención de la columna **Estado**: `Cumple` = implementado y testeado;
`Parcial` = implementado pero con un hueco (sin test, sin cubrir un caso,
etc., explicado en una nota); `No cumple` = no implementado.

## 1. Reglas de negocio numeradas (RN-01 a RN-21)

| RN | Caso de uso | Endpoint(s) | Service y método | Test(s) | Request de Bruno | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| RN-01: solo usuarios registrados y activos pueden loguearse | CU-01 | `POST /api/auth/login` | `AuthService.LoginAsync` | `LoginAsync_ConEmailInexistente_LanzaCredencialesInvalidasException`, `LoginAsync_ConUsuarioInactivo_LanzaCredencialesInvalidasException`, `Login_WithUnknownEmail_Returns401Unauthorized` | `Auth/Login con credenciales invalidas (401)` | Cumple |
| RN-02: contraseña ≥ 6 caracteres, almacenamiento seguro (hash) | CU-01, CU-08 | `POST/PUT /api/usuarios` | `UsuarioService.ValidarContraseña`, `PasswordHasher` (PBKDF2) | `CreateUsuarioAsync_WithShortPassword_ThrowsValidationException` | `Usuarios/Crear usuario con datos invalidos (400)` | Cumple |
| RN-03: datos obligatorios del libro (título, autor, ISBN, ubicación, stock) | CU-02 | `POST/PUT /api/libros` | `LibroService.ValidarDatos` | `CreateLibroAsync_WithMissingRequiredField_ThrowsValidationException` | `Libros/Crear libro con datos invalidos (400)` | Cumple |
| RN-04: stock no negativo | CU-02 | `POST/PUT /api/libros` | `LibroService.ValidarDatos` | `CreateLibroAsync_WithNegativeStock_ThrowsValidationException` | `Libros/Crear libro con datos invalidos (400)` | Cumple |
| RN-05: búsqueda de libros por título, autor o ISBN | CU-02, CU-03 | `GET /api/libros?busqueda=` | `LibroRepository.GetAllAsync` | `SearchLibrosAsync_WithText_ReturnsMatchingLibrosWithDisponibilidad` | `Libros/Buscar libros por texto` | Cumple |
| RN-06: disponibilidad según stock actual (`Stock > 0`) | CU-03 | `GET /api/libros/{id}` | `LibroService.MapToResponseDTO` (`Disponible`, `DisponibilidadTexto`) | `GetLibroDisponibilidadAsync_WithZeroStock_ReturnsUnavailable` | `Libros/Obtener libro por Id` | Cumple |
| RN-07: libro sin stock no puede prestarse | CU-03, CU-05 | `POST /api/prestamos` | `PrestamoService.CreateAsync` | `CreatePrestamoAsync_WhenLibroHasNoStock_ThrowsConflictException` | `Prestamos/Crear prestamo sin stock (409)` | Cumple |
| RN-08: DNI/legajo único | CU-04 | `POST/PUT /api/lectores` | `LectorService.ValidarIdentificadorUnicoAsync` + índice único | `CreateLectorAsync_WhenIdentificadorAlreadyExists_ThrowsConflictException` | `Lectores/Crear lector con identificador duplicado (409)` | Cumple |
| RN-09: no eliminar lector/libro con préstamos (**endurecida**, ver (c)) | CU-02, CU-04 | `DELETE /api/libros/{id}`, `DELETE /api/lectores/{id}` | `LibroService.DeleteAsync`, `LectorService.DeleteAsync` + `PrestamoRepository.TienePrestamosPor...Async` | `DeleteLibroAsync_WhenLibroHasLoanHistory_ThrowsConflictException`, `DeleteLectorAsync_WhenLectorHasLoanHistory_ThrowsConflictException` | `Libros/Eliminar libro con prestamos activos (409)`, `Lectores/Crear lector...` (ver integración `DeleteLibro_AfterLoanWasReturned_Returns409ConflictAndKeepsHistory`) | Cumple |
| RN-10: máximo 3 préstamos activos por lector | CU-05 | `POST /api/prestamos` | `PrestamoService.CreateAsync` (`CountActivosPorLectorAsync`) | `CreatePrestamoAsync_WhenLectorHasThreeActiveLoans_ThrowsConflictException` | — (requiere 4 préstamos encadenados; cubierto en integración `CreatePrestamo_WhenLectorHasThreeActiveLoans_Returns409Conflict`) | Cumple |
| RN-11: no prestar sin stock disponible | CU-05 | `POST /api/prestamos` | `PrestamoService.CreateAsync` | `CreatePrestamoAsync_WhenLibroHasNoStock_ThrowsConflictException` | `Prestamos/Crear prestamo sin stock (409)` | Cumple |
| RN-12: duración del préstamo de 14 días | CU-05 | `POST /api/prestamos` | `PrestamoService.CreateAsync` (`FechaVencimiento = FechaPrestamo.AddDays(14)`) | `CreatePrestamoAsync_WithValidData_SavesAndReturnsCreatedPrestamo` | `Prestamos/Crear prestamo (201)` | Cumple |
| RN-13: consistencia, sin préstamos duplicados (**antes no tenía validación explícita**, ver (c)) | CU-05 | `POST /api/prestamos` | `PrestamoService.CreateAsync` (`ExistePrestamoActivoAsync`) + índice único filtrado | `CreatePrestamoAsync_WhenLectorAlreadyHasActiveLoanOfSameLibro_ThrowsConflictException` | `Prestamos/Crear prestamo duplicado del mismo lector y libro (409)` | Cumple |
| RN-14: identificación de préstamos vencidos | CU-06 | `GET /api/prestamos/mora` | `PrestamoService.GetVencidosAsync` | `GetPrestamosVencidosAsync_ReturnsOverdueList` | `Prestamos/Listar prestamos en mora (200)` | Cumple |
| RN-15: el control de mora corresponde al bibliotecario (**ver (c)**) | CU-06 | `GET /api/prestamos/mora` | `[Authorize]` en `PrestamosController` (cualquier rol autenticado) | — | `Prestamos/Listar prestamos en mora (200)` | Parcial |
| RN-16: sin sistema de multas automáticas (MVP) | CU-06 | — | No existe código de multas (cumplimiento = ausencia intencional) | — | — | Cumple |
| RN-17: reportes básicos en el MVP (resumen, préstamos, mora) | CU-07 | `GET /api/reportes/{tipo}` | `ReporteService.GetReporteAsync` | `GetReporteAsync_WithValidType_ReturnsReportData` | `Reportes/Reporte resumen (200)` | Cumple |
| RN-18: estadísticas avanzadas fuera de alcance | CU-07 | — | No existe código de estadísticas avanzadas (cumplimiento = ausencia intencional) | — | — | Cumple |
| RN-19: solo el administrador gestiona usuarios | CU-08 | `/api/usuarios/*` | `[Authorize(Roles = "Administrador")]` en `UsuariosController` | — (lo resuelve el framework) | `Usuarios/Listar usuarios como bibliotecario (403)` | Cumple |
| RN-20: email, contraseña y rol obligatorios (**+formato/largo de email, ver (c)**) | CU-08 | `POST/PUT /api/usuarios` | `UsuarioService.ValidarEmail`, `ParseRol` | `CreateUsuarioAsync_WithMissingEmail_ThrowsValidationException`, `CreateUsuarioAsync_WithInvalidRol_ThrowsValidationException` | `Usuarios/Crear usuario con datos invalidos (400)` | Cumple |
| RN-21: contraseña ≥ 6 caracteres, almacenamiento seguro | CU-08 | `POST/PUT /api/usuarios` | `UsuarioService.ValidarContraseña` | `CreateUsuarioAsync_WithShortPassword_ThrowsValidationException` | `Usuarios/Crear usuario con datos invalidos (400)` | Cumple |

## 2. Reglas técnicas agregadas en este trabajo (sin número de RN en el documento original)

| Regla | Caso de uso relacionado | Endpoint(s) | Service y método | Test(s) | Request de Bruno | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| Formato de email válido (lector) | CU-04 | `POST/PUT /api/lectores` | `LectorService.ValidarDatos` + `EmailValidator` | `CreateLectorAsync_WithInvalidEmailFormat_ThrowsValidationException` | `Lectores/Crear lector con email invalido (400)` | Cumple |
| Formato de email válido (usuario) | CU-08 | `POST/PUT /api/usuarios` | `UsuarioService.ValidarEmail` + `EmailValidator` | `CreateUsuarioAsync_WithInvalidEmail_ThrowsValidationException` | `Usuarios/Crear usuario con email duplicado (409)` (comparte DTO inválido) | Cumple |
| Email normalizado (Trim + minúsculas) y comparación sin distinguir mayúsculas/espacios | CU-04, CU-08 | `POST/PUT /api/lectores`, `/api/usuarios` | `LectorService`, `UsuarioService` | `CreateLectorAsync_NormalizesEmailWithTrimAndLowerCase`, `CreateUsuarioAsync_WhenEmailDuplicadoWithDifferentCaseAndSpaces_ThrowsConflictException` | `Lectores/Crear lector con email duplicado (409)`, `Usuarios/Crear usuario con email duplicado (409)` | Cumple |
| Formato de ISBN válido (ISBN-10/13 con dígito verificador) | CU-02 | `POST/PUT /api/libros` | `LibroService.ValidarDatos` + `IsbnValidator` | `CreateLibroAsync_WithInvalidIsbn_ThrowsValidationException` | `Libros/Crear libro con isbn invalido (400)` | Cumple |
| ISBN único, normalizado sin guiones/espacios | CU-02 | `POST/PUT /api/libros` | `LibroService.ValidarIsbnUnicoAsync` + índice único | `CreateLibroAsync_WhenIsbnAlreadyExists_ThrowsConflictException`, `CreateLibroAsync_WhenIsbnDuplicadoWithHyphensAndSpaces_ThrowsConflictException` | `Libros/Crear libro con isbn duplicado (409)` | Cumple |
| Email de lector único | CU-04 | `POST/PUT /api/lectores` | `LectorService.ValidarEmailUnicoAsync` + índice único | `CreateLectorAsync_WhenEmailAlreadyExists_ThrowsConflictException` | `Lectores/Crear lector con email duplicado (409)` | Cumple |
| Identificador de lector normalizado (Trim + mayúsculas) para que la unicidad no distinga mayúsculas/espacios | CU-04 | `POST/PUT /api/lectores` | `LectorService.ValidarDatos` | `CreateLectorAsync_WhenIdentificadorDuplicadoWithDifferentCaseAndSpaces_ThrowsConflictException` | `Lectores/Crear lector con identificador duplicado (409)` | Cumple |
| Largos máximos por campo (Titulo 200, Autor 150, Ubicacion 50, Nombre/Apellido 100, Email 150, Identificador 20) | CU-02, CU-04, CU-08 | `POST/PUT` de libros/lectores/usuarios | `LibroService`, `LectorService`, `UsuarioService` + `HasMaxLength` en `BiblioGestDbContext` | `CreateLibroAsync_WithTooLongTitulo_ThrowsValidationException`, `CreateLectorAsync_WithTooLongTitulo_ThrowsValidationException`, `CreateUsuarioAsync_WithTooLongEmail_ThrowsValidationException` | `Libros/Crear libro con texto demasiado largo (400)`, `Lectores/Crear lector con texto demasiado largo (400)` | Cumple |
| `LectorId`/`LibroId` vacíos (`Guid.Empty`) → 400 en vez de 404 | CU-05 | `POST /api/prestamos` | `PrestamoService.CreateAsync` | `CreatePrestamoAsync_WithEmptyGuid_ThrowsValidationException` | `Prestamos/Crear prestamo con guid vacio (400)` | Cumple |
| Borrar libro/lector con **cualquier** préstamo en su historial (no solo activos) → 409; el historial nunca se borra | CU-02, CU-04 | `DELETE /api/libros/{id}`, `DELETE /api/lectores/{id}` | `LibroService.DeleteAsync`, `LectorService.DeleteAsync`, `PrestamoRepository.TienePrestamosPorLibroAsync`/`TienePrestamosPorLectorAsync` | `DeleteLibro_AfterLoanWasReturned_Returns409ConflictAndKeepsHistory`, `DeleteLector_AfterLoanWasReturned_Returns409ConflictAndKeepsHistory` | `Libros/Eliminar libro con prestamos activos (409)` | Cumple |
| Un lector no puede tener dos préstamos activos del mismo libro | CU-05 | `POST /api/prestamos` | `PrestamoService.CreateAsync` (`ExistePrestamoActivoAsync`) + índice único filtrado `Estado='Activo'` | `CreatePrestamoAsync_WhenLectorAlreadyHasActiveLoanOfSameLibro_ThrowsConflictException` | `Prestamos/Crear prestamo duplicado del mismo lector y libro (409)` | Cumple |
| Atomicidad: préstamo/devolución + cambio de stock en una misma transacción | CU-05 | `POST /api/prestamos`, `PUT /api/prestamos/{id}/devolucion` | `PrestamoService.CreateAsync`/`RegistrarDevolucionAsync` + `IUnitOfWork` | `CreatePrestamoAsync_WithValidData_SavesAndReturnsCreatedPrestamo` (verifica stock consistente tras la operación) | `Prestamos/Crear prestamo (201)` | Cumple (sin test de concurrencia real, ver (a)) |
| Un administrador no puede darse de baja ni desactivarse a sí mismo | CU-08 | `PUT/DELETE /api/usuarios/{id}` | `UsuarioService.UpdateAsync`/`DeleteAsync` (compara con el claim `NameIdentifier`) | `UpdateUsuarioAsync_WhenAdminDeactivatesHimself_ThrowsConflictException`, `DeleteUsuarioAsync_WhenAdminDeletesHimself_ThrowsConflictException` | `Usuarios/Admin se da de baja a si mismo (409)`, `Usuarios/Admin se desactiva a si mismo (409)` | Cumple |
| No se puede desactivar ni cambiar el rol del último administrador activo | CU-08 | `PUT/DELETE /api/usuarios/{id}` | `UsuarioService.UpdateAsync`/`DeleteAsync` (`CountAdministradoresActivosAsync`) | `UpdateUsuarioAsync_WhenDeactivatingLastActiveAdmin_ThrowsConflictException`, `UpdateUsuarioAsync_WhenChangingRoleOfLastActiveAdmin_ThrowsConflictException`, `DeleteUsuarioAsync_WhenLastActiveAdmin_ThrowsConflictException` | — (ver (a): el camino "otro admin distinto" requiere un token ya emitido de un admin luego desactivado; solo se prueba a nivel unitario) | Parcial |
| Mapeo centralizado de excepciones (400/404/409/401/500) | Todos | Todos | `ExceptionMiddleware` | `UnhandledException_Returns500InternalServerError` (resto cubierto indirectamente por cada test de 400/404/409/401) | — | Cumple |
| Campos calculados de préstamo (`estado`, `diasRestantes`, `diasDeAtraso`, `mensaje`) resueltos por la API con un único `TimeProvider` | CU-05, CU-06, CU-07 | `POST /api/prestamos`, `GET /api/prestamos/{id}`, `GET /api/prestamos/mora`, `GET /api/reportes/{tipo}` | `PrestamoMapper.ToResponseDTO` | `PrestamoMapperTests` (6 tests: activo, vence hoy, vencido, devuelto a tiempo, devuelto con atraso, singular/plural) | `Prestamos/Crear prestamo (201)` (assert agregado), `Reportes/Reporte de mora (200)` | Cumple |
| Texto de disponibilidad de libro resuelto por la API (`disponibilidadTexto`) | CU-03 | `GET /api/libros`, `GET /api/libros/{id}` | `LibroService.MapToResponseDTO` | `SearchLibrosAsync_WithText_ReturnsMatchingLibrosWithDisponibilidad` | `Libros/Obtener libro por Id` | Cumple |

## 3. Requerimientos no funcionales

| Requerimiento | Dónde se aplica | Implementación | Test(s) | Request de Bruno | Estado |
| --- | --- | --- | --- | --- | --- |
| Autenticación JWT obligatoria en endpoints protegidos | Libros, Lectores, Préstamos, Reportes, Usuarios | `[Authorize]` + `AddJwtBearer` en `Program.cs` | `ProtectedEndpoint_WithoutToken_Returns401Unauthorized` | `Libros/Listar libros sin token (401)` | Cumple |
| Token rechazado si está vencido o mal firmado | Todos los endpoints protegidos | `TokenValidationParameters` (`ValidateLifetime`, `ValidateIssuerSigningKey`) | `ProtectedEndpoint_WithExpiredToken_Returns401Unauthorized`, `ProtectedEndpoint_WithTamperedSignature_Returns401Unauthorized`, `ProtectedEndpoint_WithTokenEditedByHand_Returns401Unauthorized` | `Libros/Listar libros con token invalido (401)` | Cumple |
| El JWT no expone datos sensibles (sin hash de contraseña) | Login | `AuthService.GenerateJwtToken` solo incluye `NameIdentifier`, `Email`, `Role` | Verificado manualmente (ver guía de jwt.io en el README) | — | Cumple |
| Autorización por rol (Administrador vs Bibliotecario) | `/api/usuarios/*` | `[Authorize(Roles = "Administrador")]` | `GetUsuarios_AsBibliotecario_Returns403Forbidden` | `Usuarios/Listar usuarios como bibliotecario (403)` | Cumple |
| Validaciones de entrada consistentes devuelven 400 con mensaje claro en español | Todos los `Create`/`Update` | Cada `*Service.ValidarDatos`/`ValidarEmail`/etc. | Toda la batería de tests `*_Throws*Exception` de 400 | Todos los `(400)` de Bruno | Cumple |
| No se permiten registros duplicados (ISBN, identificador, email de lector, email de usuario, préstamo activo repetido) | Libros, Lectores, Usuarios, Préstamos | Índices únicos en `BiblioGestDbContext` + validación en el service | Ver fila "Reglas técnicas" de arriba | Todos los `(409)` de duplicados en Bruno | Cumple |
| Códigos HTTP correctos de punta a punta | Todos | `ExceptionMiddleware` (400/404/409/401/500) | Todos los tests de integración (verifican `response.StatusCode`) | Todos los `assert { res.status: ... }` de Bruno | Cumple |
| "La lógica la resuelve la API, el frontend solo muestra" (cálculos de fechas, estados, mensajes) | Préstamos, Libros, Reportes | `PrestamoMapper` + `TimeProvider`, `LibroService.MapToResponseDTO` | `PrestamoMapperTests`, `CreatePrestamo_ReturnsDiasRestantesYMensajeCalculados` | `Prestamos/Crear prestamo (201)` | Cumple |

## (a) Reglas o flujos de los CU sin implementar o sin test

1. **RN-15 (CU-06): "el control de mora corresponde al bibliotecario"** no tiene
   un test que verifique que un rol distinto de Bibliotecario quede afuera —
   porque, a propósito, también se le permite a Administrador (ver (c)). No es
   una falta de implementación sino una decisión de diseño no reflejada en un
   test negativo.
2. **"Último administrador activo" desde un caller distinto del propio
   administrador** (ver tabla de reglas técnicas) solo tiene test unitario
   (mockeando el repositorio). No hay test de integración porque, en la
   práctica, para llegar a ese estado con el flujo HTTP real hace falta un
   token JWT emitido antes de que ese mismo usuario fuera desactivado (los
   tokens son stateless y no se revocan al desactivar la cuenta) — un
   escenario válido pero deliberadamente no reproducido en un test de
   integración por la complejidad de orquestarlo. Si se quiere cerrar este
   hueco, se podría agregar una lista de revocación de tokens (fuera de
   alcance de este trabajo).
3. **Atomicidad (transacciones)**: se verifica que, tras una operación
   exitosa, el préstamo y el stock queden consistentes, pero no hay un test
   que fuerce una falla a mitad de camino (por ejemplo, mockeando que
   `UpdateAsync` del libro lance una excepción) para comprobar que la
   transacción hace rollback. Sería una mejora natural si se agrega más
   tiempo de testing.
4. **RN-16/RN-18** (sin multas automáticas, sin estadísticas avanzadas): no
   son faltas — son exclusiones de alcance documentadas en el propio CU, y
   el código correctamente no las implementa.

## (b) Endpoints o funcionalidades del código sin CU asociado

No se identificaron. Todos los endpoints de `AuthController`,
`LibrosController`, `LectoresController`, `PrestamosController`,
`ReportesController` y `UsuariosController` corresponden a un paso o flujo
alternativo de alguno de los 8 CU documentados.

## (c) Diferencias entre lo que dice el CU y lo que hace el código

1. **RN-09 (CU-02/CU-04) — "no eliminar con préstamos activos" → ahora "con
   historial de préstamos".** El CU original solo contemplaba préstamos
   **activos**; el código (ya corregido en este trabajo) bloquea el borrado
   si el libro/lector tiene **cualquier** préstamo, incluso ya devuelto. Esto
   corrige un bug real: antes, borrar un libro/lector con préstamos ya
   devueltos no estaba bloqueado a nivel de servicio y la FK con
   `DeleteBehavior.Restrict` hacía fallar `SaveChanges`, devolviendo un
   **500** en vez de un **409**. Los CU-02 y CU-04 ya se actualizaron para
   reflejar la regla endurecida.
2. **RN-13 (CU-05) — "sin préstamos duplicados" no tenía código.** El CU
   decía que esta regla ya estaba implementada, pero no existía ninguna
   validación contra préstamos activos duplicados del mismo lector+libro
   antes de este trabajo. Se agregó `PrestamoActivoDuplicadoException` (409)
   y un índice único filtrado en la base.
3. **RN-15 (CU-06) — "corresponde al bibliotecario".** El código permite que
   tanto Administrador como Bibliotecario consulten `/api/prestamos/mora`
   (cualquier usuario autenticado). Se interpretó que Administrador, al
   tener más permisos, también puede hacer lo que puede un Bibliotecario;
   el CU no lo prohíbe explícitamente, pero tampoco lo menciona.
4. **Validaciones nuevas no contempladas en ningún CU**: formato de ISBN,
   formato de email, largos máximos por campo, normalización de email e
   identificador, y el Guid vacío en préstamos. Ningún CU las menciona
   porque apuntan a "no guardar datos sucios", un requerimiento transversal
   pedido directamente para este trabajo, no una regla de negocio específica
   de un CU. Quedan documentadas en la sección 2 de esta matriz.
5. **Auto-baja y último administrador activo (CU-08)**: son reglas nuevas,
   no contempladas en la especificación original de CU-08. Ya se agregaron
   como flujos 3c-1 y 3c-2 en `cu-08-gestionar-usuarios-sistema.md`.
6. **Campos calculados de préstamo y disponibilidad de libro**: el CU-05
   especifica que la respuesta de un préstamo trae `estado` (Activo/
   Devuelto), pero no mencionaba `Vencido` ni `Devuelto con atraso` como
   valores posibles, ni los campos `diasRestantes`/`diasDeAtraso`/`mensaje`.
   Esto responde al requerimiento de este trabajo de que "la API resuelve
   todo, el frontend solo muestra"; los CU-05/06/07 y CU-03 ya se
   actualizaron para reflejarlo.
7. **`ExceptionMiddleware` centralizado**: antes, cada controller mapeaba
   excepciones a códigos HTTP con `try/catch` repetido; ahora el middleware
   lo hace una sola vez para toda la API. El comportamiento observable
   (los códigos HTTP) no cambió, pero el mecanismo sí, y los controllers ya
   no tienen lógica de mapeo.
