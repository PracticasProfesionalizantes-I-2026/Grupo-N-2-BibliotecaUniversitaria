# Caso de Uso: Gestionar Libros

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso.pdf`.
> Reglas de negocio RN-03 (datos obligatorios del libro), RN-04 (stock no
> negativo) y RN-05 (búsqueda por título/autor/ISBN) **implementadas** en
> `LibroService` / `LibroRepository`. La matriz de trazabilidad de más abajo
> refleja los tests que existen realmente en
> `tests/BiblioGest.UnitTests/LibroServiceTests.cs` y
> `tests/BiblioGest.IntegrationTests/LibrosControllerTests.cs`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Gestionar Libros |
| **Actor Principal** | Bibliotecario / Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Bibliotecario/Administrador → mantener actualizado el catálogo y el stock de libros; Lector → poder encontrar y consultar libros disponibles para solicitar en préstamo |
| **Disparador (Trigger)** | El bibliotecario o administrador necesita registrar, modificar, eliminar o consultar información de un libro del catálogo |
| **Prioridad / Frecuencia** | Alta; uso frecuente (mantenimiento habitual del catálogo) |
| **Reglas de negocio relacionadas** | RN-03 (datos obligatorios del libro); RN-04 (stock no negativo); RN-05 (búsqueda por título, autor o ISBN) |

---

### 1. BREVE DESCRIPCIÓN
Permite registrar, modificar, eliminar y consultar libros dentro del sistema, manteniendo
actualizada la información del catálogo y del stock.

### 2. PRECONDICIONES
- El usuario debe haber iniciado sesión con un estado de autenticación activo (Token
  válido).
- El usuario debe tener permisos para gestionar libros (rol Bibliotecario o
  Administrador).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201)
1. El usuario ingresa a la gestión de libros: el Actor envía una petición
   `GET /api/libros` a la **Capa de Presentación**.
2. El Sistema consulta la **Capa de Persistencia** y muestra el listado de libros
   registrados con código **HTTP 200**.
3. El usuario selecciona la acción a realizar: registrar (`POST /api/libros`), modificar
   (`PUT /api/libros/{id}`), eliminar (`DELETE /api/libros/{id}`) o consultar un libro
   puntual (`GET /api/libros/{id}`).
4. La **Capa de Presentación** solicita o muestra la información correspondiente a la
   acción elegida y valida que el formato de los datos (esquema) sea correcto.
5. El usuario confirma la operación.
6. La **Capa de Negocio** valida los datos ingresados y verifica las reglas de negocio
   aplicables (RN-03, RN-04).
7. El Sistema persiste los cambios en la **Capa de Persistencia** (tabla `Libros`) y
   devuelve un código **HTTP 201** (alta) o **200** (modificación/baja) con la
   información resultante.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Registrar libro (HTTP 201 Created):**
  1. El usuario selecciona la opción de registrar libro y envía `POST /api/libros` con
     título, autor, ISBN, ubicación y cantidad de stock.
  2. La Capa de Negocio valida los datos conforme a **RN-03**.
  3. El Sistema registra el libro y devuelve **HTTP 201 Created** con los datos del
     nuevo libro.

* **3b. Modificar libro (HTTP 200 OK):**
  1. El usuario selecciona un libro existente y envía `PUT /api/libros/{id}` con los
     datos a modificar.
  2. La Capa de Negocio valida los cambios conforme a **RN-03** y **RN-04**.
  3. El Sistema actualiza el registro y devuelve **HTTP 200 OK** con los datos
     actualizados.

* **3c. Eliminar libro (HTTP 200 OK):**
  1. El usuario selecciona un libro existente y envía `DELETE /api/libros/{id}`.
  2. La Capa de Negocio verifica si el libro posee préstamos activos asociados.
  3. Si no posee préstamos activos, el Sistema elimina o desactiva el registro y
     devuelve **HTTP 200 OK**.

* **3c-1. Libro con préstamos activos (HTTP 409 Conflict):**
  1. Si en el Paso 2 del flujo 3c el libro posee préstamos activos.
  2. La Capa de Negocio impide la eliminación del registro.
  3. El Sistema devuelve un código **409 Conflict** indicando que el libro no puede
     eliminarse mientras tenga préstamos activos. Fin del caso de uso.

* **4a. Datos obligatorios incompletos (HTTP 400 Bad Request):**
  1. Si en el Paso 4 el Sistema detecta que faltan datos obligatorios (título, autor,
     ISBN, ubicación o cantidad de stock).
  2. La Capa de Presentación rechaza la petición por error de validación.
  3. El Sistema devuelve un código **400 Bad Request** informando los campos
     requeridos. Fin del caso de uso.

* **6a. Stock negativo (HTTP 400 Bad Request):**
  1. Si en el Paso 6 la cantidad de stock ingresada es negativa, violando **RN-04**.
  2. La Capa de Negocio rechaza la operación.
  3. El Sistema devuelve un código **400 Bad Request** indicando que el stock no puede
     ser negativo. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. La consulta de libros (Paso 1-2) admite búsqueda por título, autor o ISBN
   (**RN-05**), sin alterar el resultado esperado (listado filtrado con código
   **HTTP 200**).

### 6. POSTCONDICIONES
- La información de los libros queda actualizada en el catálogo.
- El stock queda registrado de forma consistente.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta del listado/detalle, modificación o baja exitosa de un libro. |
| `201` | Created | Confirmación de alta de un nuevo libro en el catálogo. |
| `400` | Bad Request | Datos obligatorios faltantes o stock negativo (RN-04). |
| `409` | Conflict | Intento de eliminar un libro con préstamos activos. |

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario | Test integración |
| --- | --- | --- | --- |
| 3a. Registrar libro | `201 Created` | `CreateLibroAsync_WithValidData_SavesAndReturnsCreatedLibro` | `CreateLibro_ReturnsSuccessAndCreatedLibro` |
| 3b. Modificar libro | `200 OK` | `UpdateLibroAsync_WithValidData_UpdatesLibro` | `UpdateLibro_ReturnsSuccessAndUpdatedLibro` |
| 3c. Eliminar libro | `200 OK` | `DeleteLibroAsync_WithoutActiveLoans_DeletesLibro` | `DeleteLibro_WithoutActiveLoans_Returns200OK` |
| 3c-1. Libro con préstamos activos | `409 Conflict` | `DeleteLibroAsync_WhenLibroHasActiveLoans_ThrowsConflictException` | `DeleteLibro_WithActiveLoans_Returns409Conflict` |
| 4a. Datos obligatorios incompletos | `400 Bad Request` | — (validación de esquema vía DataAnnotations en los DTOs) | `CreateLibro_WithMissingRequiredField_Returns400BadRequest`, `CreateLibro_WithTituloTooLong_Returns400BadRequest` |
| 6a. Stock negativo | `400 Bad Request` | `CreateLibroAsync_WithNegativeStock_ThrowsValidationException` | `CreateLibro_WithNegativeStock_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso tiene al menos un test unitario y uno de
> integración.
