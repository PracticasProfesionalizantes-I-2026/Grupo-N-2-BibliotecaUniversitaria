# Caso de Uso: Gestionar Préstamos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso.pdf`.
> Reglas de negocio RN-10 (máximo 3 préstamos activos por lector), RN-11 (no prestar
> sin stock), RN-12 (14 días de préstamo) y RN-13 (consistencia, sin préstamos
> duplicados) **implementadas** en `PrestamoService`. La matriz de trazabilidad de
> más abajo refleja los tests que existen realmente en
> `tests/BiblioGest.UnitTests/PrestamoServiceTests.cs` y
> `tests/BiblioGest.IntegrationTests/PrestamosControllerTests.cs`; las filas sin test
> de integración dedicado quedan marcadas como pendientes.

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

* **3a. Lector con 3 préstamos activos (HTTP 409 Conflict):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el lector ya tiene 3 libros
     retirados, violando **RN-10**.
  2. El Sistema impide registrar un nuevo préstamo.
  3. El Sistema devuelve un código **409 Conflict** informando el motivo de la
     restricción. Fin del caso de uso.

* **3b. Lector con mora (HTTP 409 Conflict):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el lector posee préstamos vencidos.
  2. El Sistema impide registrar un nuevo préstamo hasta regularizar la situación.
  3. El Sistema devuelve un código **409 Conflict** informando la restricción. Fin del
     caso de uso.

* **5a. Libro sin stock (HTTP 409 Conflict):**
  1. Si en el Paso 5 la Capa de Negocio detecta que no hay ejemplares disponibles,
     violando **RN-11**.
  2. El Sistema impide registrar el préstamo.
  3. El Sistema devuelve un código **409 Conflict** informando que el libro no se
     encuentra disponible. Fin del caso de uso.

* **6a. Registrar devolución (HTTP 200 OK):**
  1. El usuario busca el préstamo activo del lector y envía
     `PUT /api/prestamos/{id}/devolucion`.
  2. El Sistema muestra los datos del préstamo.
  3. El usuario confirma la devolución del libro.
  4. El Sistema registra la devolución en la **Capa de Persistencia**.
  5. El Sistema aumenta una unidad del stock del libro.
  6. El Sistema actualiza el estado del préstamo y devuelve un código **HTTP 200 OK**.

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
| `200` | OK | Confirmación de registro de una devolución. |
| `201` | Created | Confirmación de registro de un nuevo préstamo. |
| `404` | Not Found | El lector o el libro indicado no existen en el sistema. |
| `409` | Conflict | Lector con 3 préstamos activos (RN-10), lector en mora, o libro sin stock disponible (RN-11). |

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario | Test integración |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CreatePrestamoAsync_WithValidData_SavesAndReturnsCreatedPrestamo` | `CreatePrestamo_ReturnsSuccessAndCreatedPrestamo` |
| 1a. Lector o libro inexistente | `404 Not Found` | `CreatePrestamoAsync_WhenLectorOrLibroNotFound_ThrowsNotFoundException` | `CreatePrestamo_WithUnknownLectorOrLibro_Returns404NotFound` |
| 3a. Lector con 3 préstamos activos | `409 Conflict` | `CreatePrestamoAsync_WhenLectorHasThreeActiveLoans_ThrowsConflictException` | — (pendiente) |
| 3b. Lector con mora | `409 Conflict` | `CreatePrestamoAsync_WhenLectorHasOverdueLoans_ThrowsConflictException` | — (pendiente) |
| 5a. Libro sin stock | `409 Conflict` | `CreatePrestamoAsync_WhenLibroHasNoStock_ThrowsConflictException` | `CreatePrestamo_WithoutStock_Returns409Conflict` |
| 6a. Registrar devolución | `200 OK` | `RegisterDevolucionAsync_WithValidData_UpdatesStockAndPrestamoState` | `RegisterDevolucion_ReturnsSuccessAndUpdatedPrestamo` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Las filas
> marcadas como "pendiente" ya están probadas a nivel unitario pero todavía no
> tienen test de integración dedicado; quedan para una iteración futura.
