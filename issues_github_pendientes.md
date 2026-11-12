# Issues Pendientes de GitHub — Auditoría Integral BiblioGest

Este documento consolida los hallazgos técnicos, brechas funcionales y oportunidades de mejora detectados durante la auditoría del Ciclo de Vida de Desarrollo (SDLC) del proyecto **BiblioGest**. Está estructurado para ser copiado y administrado directamente como issues o tareas en el repositorio de GitHub.

---

### 🔴 RIESGO ALTO (Crítico para la ejecución y completitud core)
*Incluye bugs bloqueantes, vulnerabilidades de seguridad graves y requerimientos principales o casos de uso core no implementados.*

- [ ] **[Seguridad] Extraer la clave secreta JWT y credenciales de los archivos de configuración versionados**
  * **Archivo:** [`API/appsettings.json`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/appsettings.json#L4-L9) y [`API/Program.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/Program.cs#L66-L70)
  * **Problema:** La clave simétrica de firma del token JWT (`"UnaClaveSecretaBienLargaYDificilDeAdivinar123!"`) se encuentra harcodeada y comiteada en el repositorio público/compartido. Cualquiera con acceso al código puede firmar tokens arbitrarios con rol `Administrador` y vulnerar la API por completo.
  * **Acción correctiva:** Mover la clave a Variables de Entorno (`JWT__KEY`) o `dotnet user-secrets` en desarrollo. En `Program.cs`, validar que la clave no sea nula ni tome valores por defecto inseguros al iniciar en entornos productivos.

- [ ] **[Concurrencia / Lógica Core] Condición de carrera (Race Condition) en asignación de préstamos y reserva de último stock**
  * **Archivo:** [`BusinessLogic/Services/PrestamoService.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/PrestamoService.cs#L60-L88) y [`BusinessLogic/Services/ReservaService.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/ReservaService.cs#L55-L75)
  * **Problema:** Si dos peticiones simultáneas intentan crear un préstamo para un libro con `Stock = 1`, ambas leen `Stock > 0` concurrentemente debido a que no existe aislamiento transaccional explícito ni bloqueo optimista a nivel de base de datos. Como resultado, ambas peticiones descuentan stock y dejan el stock en `-1` o prestan el mismo ejemplar dos veces.
  * **Acción correctiva:** Incorporar un token de concurrencia optimista (`RowVersion` / `[Timestamp]`) en la entidad `Libro`, o gestionar una transacción de base de datos (`await _context.Database.BeginTransactionAsync()`) asegurando lecturas consistentes antes de decrementar el stock.

- [ ] **[Atomicidad Transaccional] Posible inconsistencia de estado al devolver ejemplares con cola de reservas**
  * **Archivo:** [`BusinessLogic/Services/PrestamoService.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/PrestamoService.cs#L170-L190) y [`BusinessLogic/Services/ColaDeReservas.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/ColaDeReservas.cs#L45-L60)
  * **Problema:** En `CerrarPrestamoAsync`, se actualiza primero el préstamo (`_prestamoRepository.UpdateAsync`), luego la reserva o el stock del libro (`_reservaRepository.UpdateAsync` / `_libroRepository.UpdateAsync`), y finalmente la suspensión del lector (`_lectorRepository.UpdateAsync`). Cada llamada ejecuta `SaveChangesAsync()` de forma independiente. Si una de las operaciones intermedias falla (red, desconexión de BD, validación), el préstamo queda como `Devuelto` pero el stock nunca se incrementa ni se asigna la reserva.
  * **Acción correctiva:** Envolver la operación completa de cierre de préstamo en un `Unit of Work` o transacción de base de datos atómica que realice rollback total ante excepciones.

---

### 🟡 RIESGO MEDIO (Mantenibilidad y Flujos Secundarios)
*Incluye requerimientos secundarios faltantes, brechas de lógica diseño vs código, o code smells severos (ej. clases Dios, fuerte acoplamiento).*

- [ ] **[Brecha Diseño vs. Código] Clarificar rol y acceso del actor 'Lector / Alumno' en la documentación y CU-03**
  * **Archivo:** [`Documentos/Casos de Uso/cu-03-consultar-disponibilidad-libros.md`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/Documentos/Casos%20de%20Uso/cu-03-consultar-disponibilidad-libros.md#L15-L25) y [`API/Controllers/LibrosController.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/Controllers/LibrosController.cs#L10-L15)
  * **Problema:** CU-03 especifica al *Lector/Alumno* como actor secundario consultando disponibilidad en el sistema. Sin embargo, en la API `LibrosController` tiene el atributo global `[Authorize]`, impidiendo el acceso a cualquier actor que no cuente con credenciales de `Usuario` del sistema (Administrador o Bibliotecario).
  * **Acción correctiva:** Alinear la documentación especificando formalmente que la consulta la realiza el bibliotecario a petición del lector (proxy humano), o habilitar `[AllowAnonymous]` en el endpoint de búsqueda `GET /api/libros` si el catálogo fuera público.

- [ ] **[Integridad Referencial] Inconsistencia entre DeleteBehavior de Reservas y validaciones en Servicios**
  * **Archivo:** [`DataAccess/BiblioGestDbContext.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/DataAccess/BiblioGestDbContext.cs#L67-L77) vs. [`BusinessLogic/Services/LibroService.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/LibroService.cs#L75-L95)
  * **Problema:** En el `DbContext`, las relaciones de `Reserva` con `Libro` y `Lector` están configuradas con `DeleteBehavior.Cascade`, lo que permitiría que el motor borre automáticamente las reservas si se borra el padre. No obstante, en `LibroService` y `LectorService` se prohíbe el borrado explícito si existen reservas activas (`LibroConReservasActivasException`).
  * **Acción correctiva:** Homogeneizar la configuración en `DbContext` declarando `DeleteBehavior.Restrict` para `Reserva.Libro` y `Reserva.Lector`, garantizando que la base de datos aplique la misma restricción defensiva que la capa de negocio.

- [ ] **[NFR de Escalabilidad] Ausencia de paginación en endpoints de consulta masiva**
  * **Archivo:** [`API/Controllers/LibrosController.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/Controllers/LibrosController.cs#L20-L26), [`API/Controllers/LectoresController.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/Controllers/LectoresController.cs#L20-L26) y [`API/Controllers/PrestamosController.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/Controllers/PrestamosController.cs#L30-L36)
  * **Problema:** Los métodos `GetAll` retornan todos los registros existentes en memoria (`ToListAsync()`). En un escenario real con miles de libros o lectores, este comportamiento provocará degradación de rendimiento, saturación de ancho de banda y picos de consumo de RAM.
  * **Acción correctiva:** Incorporar parámetros de paginación opcionales (`pageNumber`, `pageSize`) con valores por defecto razonables (ej. 20 o 50 registros por página) en los DTOs de consulta y repositorios (`Skip`/`Take`).

- [ ] **[Lógica de Dominio] Dependencia acoplada en ColaDeReservas mediante bucle de consultas**
  * **Archivo:** [`BusinessLogic/Services/ColaDeReservas.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/ColaDeReservas.cs#L21-L36)
  * **Problema:** `ProcesarAsignacionesVencidasAsync` realiza un ciclo `while (vencida is not null)` donde en cada iteración ejecuta operaciones de base de datos individuales y vuelve a llamar a `LiberarEjemplarAsync`, el cual vuelve a consultar la lista activa. Aunque funcional para colas pequeñas, genera consultas N+1 repetidas dentro de la misma llamada.
  * **Acción correctiva:** Refactorizar la cascada de vencimientos para procesar las transiciones en un único paso en memoria antes de persistir los cambios acumulados.

---

### 🟢 RIESGO BAJO (Deuda técnica y Mejoras Menores)
*Incluye casos borde, validaciones faltantes, code smells menores y convenciones de formato/nomenclatura.*

- [ ] **[Code Smell / Limpieza] Eliminación de archivo BOM (Byte Order Mark) y directivas using duplicadas**
  * **Archivo:** [`BusinessLogic/Services/UsuarioService.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/UsuarioService.cs#L1) y [`BusinessLogic/Services/PasswordHasher.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/BusinessLogic/Services/PasswordHasher.cs#L1-L6)
  * **Problema:** `UsuarioService.cs` y `PasswordHasher.cs` contienen caracteres UTF-8 BOM iniciales (`\ufeff`) y directivas `using` no globales que contrastan con el resto de la solución donde se utiliza el estilo moderno de C# 12/10 con file-scoped namespaces y sin BOM.
  * **Acción correctiva:** Guardar ambos archivos en formato UTF-8 standard (sin BOM) y normalizar el estilo de los namespaces a file-scoped namespaces (`namespace BiblioGest.BusinessLogic.Services;`).

- [ ] **[Manejo de Errores] Evitar 'Null Reference' en extracción de Claims de Usuario**
  * **Archivo:** [`API/Controllers/UsuariosController.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/Controllers/UsuariosController.cs#L98-L100)
  * **Problema:** El método `UsuarioActualId()` utiliza el operador de supresión de nulos `int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)`. Si el token llegara a carecer de este claim o fuera mal emitido, arrojará `ArgumentNullException` que desembocará en un error 500 no controlado.
  * **Acción correctiva:** Emplear `int.TryParse` y, en caso de no encontrarse el claim válido, retornar una excepción de autorización (`UnauthorizedException`) o código 401 controlado.

- [ ] **[Validación de Datos] Estandarización de validación de ISBN y normalización**
  * **Archivo:** [`DataAccess/Repositories/LibroRepository.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/DataAccess/Repositories/LibroRepository.cs#L36-L40)
  * **Problema:** La validación de existencia de ISBN en `ExisteIsbnAsync` utiliza `l.Isbn.Replace("-", "").ToUpper() == isbnSinGuiones`. Al ejecutarse en SQLite/EF Core, esto puede inhibir el uso eficiente de índices sobre la columna `Isbn` al aplicar funciones escalares sobre la columna en la cláusula `WHERE`.
  * **Acción correctiva:** Considerar persistir una columna normalizada calculada/almacenada `IsbnNormalizado` con índice único en la base de datos para optimizar las consultas de unicidad.

- [ ] **[Swagger / OpenAPI] Configurar descripciones de respuestas para códigos de error en OpenAPI**
  * **Archivo:** [`API/Controllers/PrestamosController.cs`](file:///Users/oscarspila/repos/ices2026/Grupo-N-2-BibliotecaUniversitaria/API/Controllers/PrestamosController.cs) y demás controladores
  * **Problema:** Los endpoints carecen de atributos `[ProducesResponseType(StatusCodes.Status403Forbidden)]`, `[ProducesResponseType(StatusCodes.Status409Conflict)]`, etc. Swagger UI no documenta formalmente los esquemas de error devueltos a los clientes.
  * **Acción correctiva:** Agregar las anotaciones `[ProducesResponseType]` correspondientes en las acciones de los controladores para enriquecer la documentación interactiva de Swagger.

