# Caso de Uso: Gestionar Préstamos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso.pdf`.
> Reglas de negocio RN-10 (máximo 3 préstamos activos por lector), RN-11 (no prestar
> sin stock), RN-12 (14 días de préstamo) y RN-13 (consistencia, sin préstamos
> duplicados) **implementadas** en `PrestamoService`: crear un préstamo descuenta stock
> y registrar la devolución lo repone una sola vez (una devolución repetida devuelve
> 409, ver CU-06 alternativa 7b). El límite de préstamos, la mora y la suspensión por
> atraso (nivel 2 de la matriz de complejidad) bloquean el alta con 403 Forbidden.
> Un préstamo puede marcarse como Extraviado; el lector queda bloqueado hasta reponer
> el mismo ejemplar (estado Repuesto), sin multas monetarias (nivel 1, ver 6b y 6c).
> Con stock 0 se puede reservar en cola con pre-asignación automática de 24 h al
> devolverse un ejemplar (nivel 3, ver 5b, 5c y 6d), y un préstamo se puede renovar
> hasta 2 veces si no hay cola de reservas (nivel 4, ver 6e).
> Requiere token JWT (`[Authorize]`). La matriz de
> trazabilidad lista los tests reales.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Gestionar Préstamos |
| **Actor Principal** | Bibliotecario / Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Bibliotecario/Administrador → registrar correctamente préstamos y devoluciones, manteniendo el stock consistente; Lector → poder retirar y devolver libros de forma ágil y sin errores en su historial |
| **Disparador (Trigger)** | Un lector solicita retirar o devolver un libro, o el usuario del sistema necesita administrar un préstamo |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente (operación diaria central del sistema) |
| **Reglas de negocio relacionadas** | RN-10 (máximo 3 préstamos activos por lector); RN-11 (no prestar sin stock disponible); RN-12 (duración del préstamo de 14 días); RN-13 (consistencia, sin préstamos duplicados) |

---

### 1. BREVE DESCRIPCIÓN
Permite registrar préstamos y devoluciones de libros, actualizando automáticamente el
stock y el estado del préstamo.

### 2. PRECONDICIONES
- El usuario debe haber iniciado sesión con un estado de autenticación activo (Token
  válido).
- El lector debe estar registrado en el sistema.
- El libro debe estar registrado en el sistema.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El lector solicita un préstamo al bibliotecario, o el usuario inicia la gestión del
   préstamo: el Actor envía una petición `POST /api/prestamos` a la **Capa de
   Presentación**.
2. El usuario busca al lector en el sistema (`GET /api/lectores/{id}`).
3. La **Capa de Negocio** valida que el lector exista y no tenga restricciones activas
   (RN-10, RN-13).
4. El usuario busca el libro solicitado (`GET /api/libros/{id}`).
5. La **Capa de Negocio** valida que haya stock disponible (RN-11).
6. El Sistema registra el préstamo en la **Capa de Persistencia**.
7. El Sistema calcula automáticamente la fecha de vencimiento conforme a **RN-12**.
8. El Sistema descuenta una unidad del stock del libro.
9. El usuario confirma la operación; el Sistema devuelve un código **HTTP 201 Created**
   con los datos del préstamo, informando el resultado al lector si corresponde.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Lector con 3 préstamos activos (HTTP 403 Forbidden):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el lector ya tiene 3 libros
     retirados, violando **RN-10**.
  2. El Sistema impide registrar un nuevo préstamo.
  3. El Sistema devuelve un código **403 Forbidden** informando el motivo de la
     restricción. Fin del caso de uso.

* **3b. Lector con mora (HTTP 403 Forbidden):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el lector posee préstamos vencidos.
  2. El Sistema impide registrar un nuevo préstamo hasta regularizar la situación.
  3. El Sistema devuelve un código **403 Forbidden** informando la restricción. Fin del
     caso de uso.

* **3c. Lector suspendido por devolución con atraso (HTTP 403 Forbidden):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el lector tiene una suspensión
     vigente (`SuspendidoHasta` posterior al momento actual), generada al devolver
     tarde un préstamo anterior (ver CU-06 alternativa 7c).
  2. El Sistema impide registrar un nuevo préstamo hasta que venza la suspensión.
  3. El Sistema devuelve un código **403 Forbidden** indicando hasta cuándo dura la
     suspensión. Fin del caso de uso.

* **3d. Lector con un libro extraviado sin reponer (HTTP 403 Forbidden):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el lector tiene un préstamo en
     estado `Extraviado`.
  2. El Sistema impide registrar un nuevo préstamo hasta que reponga el ejemplar.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **3e. El lector ya tiene ese libro prestado (HTTP 409 Conflict):**
  1. Si el lector tiene un préstamo `Activo` del mismo libro.
  2. La Capa de Negocio rechaza el alta (`PrestamoActivoDuplicadoException`, RN-13).
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3f. Consultar si el lector puede pedir un préstamo (HTTP 200 OK):**
  1. Antes de registrar el préstamo, el usuario puede enviar
     `GET /api/prestamos/estado-lector/{lectorId}`.
  2. El Sistema aplica las mismas reglas que el alta (3a a 3d) y devuelve si puede
     pedir, el motivo si no puede (mismo mensaje que daría el alta) y la fecha de
     vencimiento que tendría el préstamo. Si el lector no existe responde **404**.

* **5a. Libro sin stock (HTTP 409 Conflict):**
  1. Si en el Paso 5 la Capa de Negocio detecta que no hay ejemplares disponibles,
     violando **RN-11**.
  2. El Sistema impide registrar el préstamo.
  3. El Sistema devuelve un código **409 Conflict** informando que el libro no se
     encuentra disponible. Fin del caso de uso.

* **5b. Reservar en cola un libro sin stock (HTTP 201 Created):**
  1. Si el libro no tiene stock, el usuario envía `POST /api/reservas` con el lector y
     el libro.
  2. La Capa de Negocio verifica que el stock sea 0 (si hay stock responde **409**:
     se presta directo) y que el lector no tenga otra reserva activa de ese libro (409).
  3. El Sistema registra la reserva en estado `EnCola` y devuelve **201 Created** con
     su posición en la cola (orden de llegada).
  4. El usuario puede consultar la cola con `GET /api/reservas?libroId={id}` o
     cancelar una reserva con `PUT /api/reservas/{id}/cancelacion`.

* **5c. Retirar un ejemplar pre-asignado por reserva (HTTP 201 Created):**
  1. Si el lector tiene una reserva `Asignada` vigente de ese libro, el préstamo se
     registra aunque el stock sea 0: usa el ejemplar apartado para él.
  2. La reserva pasa a `Concretada`. Los demás lectores no pueden llevarse ese
     ejemplar (para ellos el libro sigue sin stock, 409).

* **6a. Registrar devolución (HTTP 200 OK):**
  1. El usuario busca el préstamo activo del lector y envía
     `PUT /api/prestamos/{id}/devolucion`.
  2. El Sistema muestra los datos del préstamo.
  3. El usuario confirma la devolución del libro.
  4. El Sistema registra la devolución en la **Capa de Persistencia**.
  5. El Sistema aumenta una unidad del stock del libro.
  6. El Sistema actualiza el estado del préstamo y devuelve un código **HTTP 200 OK**.

* **6b. Registrar extravío (HTTP 200 OK):**
  1. El lector informa que perdió el libro de un préstamo activo y el usuario envía
     `PUT /api/prestamos/{id}/extravio`.
  2. La Capa de Negocio verifica que el préstamo esté `Activo`; si no lo está
     (`Devuelto`, `Extraviado` o `Repuesto`) devuelve **409 Conflict**.
  3. El Sistema cambia el estado a `Extraviado`. El stock **no** se repone porque el
     ejemplar no volvió a la biblioteca.
  4. Desde ese momento el lector no puede pedir préstamos (alternativa 3d) y devuelve
     **HTTP 200 OK**.

* **6c. Registrar reposición del ejemplar (HTTP 200 OK):**
  1. El lector trae un ejemplar igual al extraviado y el usuario envía
     `PUT /api/prestamos/{id}/reposicion`.
  2. La Capa de Negocio verifica que el préstamo esté `Extraviado`; si no lo está
     devuelve **409 Conflict**.
  3. El Sistema cambia el estado a `Repuesto`, registra la fecha, suma una unidad al
     stock del libro y levanta el bloqueo.
  4. Si la reposición es posterior al vencimiento, se aplica la suspensión por atraso
     (1 día por cada día, igual que en una devolución tardía, CU-06 alternativa 7c).
     Devuelve **HTTP 200 OK**.

* **6d. Devolución o reposición con lectores en la cola de reservas (HTTP 200 OK):**
  1. Si al devolver o reponer el ejemplar el libro tiene reservas `EnCola`, el
     ejemplar **no** vuelve al stock.
  2. El Sistema lo pre-asigna automáticamente al primero de la cola: la reserva pasa a
     `Asignada` con `fechaLimiteRetiro` = fecha de devolución + 24 horas.
  3. Si el lector no lo retira en ese plazo, la reserva pasa a `Vencida` y el ejemplar
     se asigna al siguiente (otras 24 h desde el vencimiento anterior) o vuelve al
     stock si no queda nadie. Esto se evalúa al consultar u operar sobre el libro,
     igual que la mora, sin tareas en segundo plano.

* **6e. Renovar préstamo (HTTP 200 OK):**
  1. El usuario envía `PUT /api/prestamos/{id}/renovacion`.
  2. La Capa de Negocio verifica que el préstamo esté `Activo` (si no, **409**), que no
     esté vencido, que tenga menos de 2 renovaciones y que no haya lectores activos
     (`EnCola` o `Asignada`) en la cola de reservas del libro. Si alguna falla responde
     **403 Forbidden**.
  3. El Sistema extiende el vencimiento 14 días, suma una renovación y devuelve
     **HTTP 200 OK**.

* **1a. Lector o libro inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 2 o el Paso 4 el lector o el libro indicado no existen en el
     sistema.
  2. La Capa de Negocio no puede continuar con la operación.
  3. El Sistema devuelve un código **404 Not Found** indicando el recurso no
     encontrado. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No se identifican variaciones de datos o mecanismo adicionales a las descriptas en el
flujo principal y alternativo._

### 6. POSTCONDICIONES
- El préstamo o la devolución quedan registrados en el sistema.
- El stock del libro queda actualizado.
- El estado del préstamo queda actualizado.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de una devolución, un extravío, una reposición, una renovación o una cancelación de reserva; consulta de la cola de reservas. |
| `201` | Created | Confirmación de registro de un nuevo préstamo o de una reserva. |
| `401` | Unauthorized | Petición sin token válido. |
| `403` | Forbidden | Lector con 3 préstamos activos (RN-10), lector en mora, lector suspendido por devolver con atraso , lector con un libro extraviado sin reponer, o renovación denegada (2 renovaciones, préstamo vencido o cola de reservas activa). |
| `404` | Not Found | El lector, el libro, el préstamo o la reserva indicados no existen en el sistema. |
| `409` | Conflict | Libro sin stock disponible (RN-11), préstamo activo duplicado del mismo libro (RN-13), devolución de un préstamo que no está activo, extravío de un préstamo que no está activo , reposición de un préstamo que no está extraviado, renovación de un préstamo no activo, reserva de un libro con stock, reserva duplicada o cancelación de una reserva no activa. |

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Código HTTP | Test unitario | Test de integración |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CreatePrestamoAsync_WithValidData_SavesAndReturnsCreatedPrestamo` | `CreatePrestamo_ReturnsSuccessAndCreatedPrestamo` |
| 1a. Lector o libro inexistente | `404 Not Found` | `CreatePrestamoAsync_WhenLectorOrLibroNotFound_ThrowsNotFoundException` | `CreatePrestamo_WithUnknownLectorOrLibro_Returns404NotFound` |
| 3a. Lector con 3 préstamos activos | `403 Forbidden` | `CreatePrestamoAsync_WhenLectorHasThreeActiveLoans_ThrowsForbiddenException` | `CreatePrestamo_WhenLectorHasThreeActiveLoans_Returns403Forbidden` |
| 3b. Lector con mora | `403 Forbidden` | `CreatePrestamoAsync_WhenLectorHasOverdueLoans_ThrowsForbiddenException` | `CreatePrestamo_WhenLectorInMora_Returns403Forbidden` |
| 3c. Lector suspendido | `403 Forbidden` | `CreatePrestamoAsync_WhenLectorIsSuspended_ThrowsForbiddenException`, `CreatePrestamoAsync_WhenSuspensionHasExpired_CreatesPrestamo` | `RegisterDevolucion_WhenLate_SuspendsLectorAndNextPrestamoReturns403Forbidden` |
| 3d. Lector con libro extraviado | `403 Forbidden` | `CreatePrestamoAsync_WhenLectorHasExtraviadoPrestamo_ThrowsForbiddenException` | `Extravio_BlocksLectorUntilReposicionAndThenRestoresStock` |
| 3e. Préstamo duplicado del mismo libro | `409 Conflict` | `CreatePrestamoAsync_WhenLectorAlreadyHasSameBookActive_ThrowsConflictException` | `CreatePrestamo_WhenLectorAlreadyHasSameBookActive_Returns409Conflict` |
| 3f. Estado del lector | `200` / `404` | `GetEstadoLectorAsync_WithoutRestrictions_CanBorrow`, `GetEstadoLectorAsync_WhenSuspended_CannotBorrowAndExplainsWhy`, `GetEstadoLectorAsync_WithUnknownLector_ThrowsNotFoundException` | `GetEstadoLector_WithFreeLector_ReturnsCanBorrow`, `GetEstadoLector_WithUnknownLector_Returns404NotFound` |
| 5a. Libro sin stock | `409 Conflict` | `CreatePrestamoAsync_WhenLibroHasNoStock_ThrowsConflictException` | `CreatePrestamo_WithoutStock_Returns409Conflict` |
| 6a. Registrar devolución | `200 OK` | `RegisterDevolucionAsync_WithValidData_UpdatesStockAndPrestamoState` | `RegisterDevolucion_ReturnsSuccessAndUpdatedPrestamo` |
| 6a-1. Devolver un préstamo extraviado | `409 Conflict` | `RegisterDevolucionAsync_WhenPrestamoIsExtraviado_ThrowsConflictException` | — |
| 6b. Registrar extravío | `200 OK` / `409` / `404` | `RegistrarExtravioAsync_WithActivePrestamo_MarksExtraviadoAndKeepsStock`, `RegistrarExtravioAsync_WhenPrestamoIsNotActive_ThrowsConflictException`, `RegistrarExtravioAsync_WithUnknownId_ThrowsNotFoundException` | `Extravio_BlocksLectorUntilReposicionAndThenRestoresStock`, `Extravio_WhenPrestamoAlreadyReturned_Returns409Conflict`, `Extravio_WithUnknownId_Returns404NotFound` |
| 6c. Registrar reposición | `200 OK` / `409` | `RegistrarReposicionAsync_WithExtraviadoPrestamo_MarksRepuestoAndRestoresStock`, `RegistrarReposicionAsync_WhenLate_SuspendsLectorForDaysLate`, `RegistrarReposicionAsync_WhenPrestamoIsNotExtraviado_ThrowsConflictException` | `Extravio_BlocksLectorUntilReposicionAndThenRestoresStock`, `Reposicion_WhenPrestamoIsNotExtraviado_Returns409Conflict` |
| 5b. Reservar en cola | `201` / `409` / `404` | `CreateReservaAsync_WhenLibroHasNoStock_QueuesReservaWithPosition`, `CreateReservaAsync_WhenLibroHasStock_ThrowsConflictException`, `CreateReservaAsync_WhenLectorAlreadyHasActiveReserva_ThrowsConflictException`, `CreateReservaAsync_WhenLectorDoesNotExist_ThrowsNotFoundException` | `CreateReserva_WhenLibroHasStock_Returns409Conflict`, `CreateReserva_WhenDuplicated_Returns409Conflict`, `CreateReserva_WithUnknownLibro_Returns404NotFound` |
| 5b-1. Cancelar reserva | `200` / `409` | `CancelarReservaAsync_WhenEnCola_CancelsWithoutReleasingCopy`, `CancelarReservaAsync_WhenAsignada_ReleasesCopyToQueueOrStock`, `CancelarReservaAsync_WhenNotActive_ThrowsConflictException` | `CancelarReserva_ThenCancelAgain_Returns200And409` |
| 5c. Retirar ejemplar pre-asignado | `201 Created` | `CreatePrestamoAsync_WithAssignedReserva_UsesHeldCopyWithoutStock` | `Reserva_WhenCopyIsReturned_IsAssignedFor24HoursAndOnlyThatReaderCanBorrowIt` |
| 6d. Devolución con cola (pre-asignación 24 h) | `200 OK` | `RegisterDevolucionAsync_WhenQueueHasReaders_AssignsCopyInsteadOfRestoringStock`, `AsignarAlSiguienteAsync_WithReaderInQueue_AssignsFor24Hours` | `Reserva_WhenCopyIsReturned_IsAssignedFor24HoursAndOnlyThatReaderCanBorrowIt` |
| 6d-1. Vence la pre-asignación | — | `ProcesarAsignacionesVencidasAsync_WhenAssignmentExpired_PassesCopyToNextInQueue`, `ProcesarAsignacionesVencidasAsync_WhenAssignmentExpiredAndQueueEmpty_ReturnsCopyToStock`, `ProcesarAsignacionesVencidasAsync_WhenNextAssignmentAlsoExpired_ChainsUntilStock`, `CreatePrestamoAsync_ProcessesExpiredAssignmentsBeforeCheckingStock` | `Reserva_WhenAssignmentExpiredAndQueueIsEmpty_ReturnsCopyToStock` |
| 6e. Renovar préstamo | `200` / `403` / `409` | `RenovarAsync_WithActivePrestamo_ExtendsDueDateFourteenDays`, `RenovarAsync_WhenAlreadyRenewedTwice_ThrowsForbiddenException`, `RenovarAsync_WhenLibroHasActiveReservas_ThrowsForbiddenException`, `RenovarAsync_WhenPrestamoIsOverdue_ThrowsForbiddenException`, `RenovarAsync_WhenPrestamoIsNotActive_ThrowsConflictException` | `Renovar_UpToTwoTimes_ThenReturns403Forbidden`, `Renovar_WhenOtherReadersAreInQueue_Returns403Forbidden`, `Renovar_WhenPrestamoWasReturned_Returns409Conflict` |
