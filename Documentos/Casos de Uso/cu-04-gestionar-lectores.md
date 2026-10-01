# Caso de Uso: Gestionar Lectores

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso.pdf`.
> Reglas de negocio RN-08 (unicidad de DNI/legajo) y RN-09 (no eliminar lector con
> préstamos activos) **implementadas** en `LectorService`. La matriz de trazabilidad
> de más abajo refleja los tests que existen realmente en
> `tests/BiblioGest.UnitTests/LectorServiceTests.cs` y
> `tests/BiblioGest.IntegrationTests/LectoresControllerTests.cs`; las filas sin test
> dedicado quedan marcadas como pendientes.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Gestionar Lectores |
| **Actor Principal** | Bibliotecario / Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Bibliotecario/Administrador → mantener actualizados los datos de los lectores habilitados a operar en el sistema; Lector → que sus datos de identificación y contacto estén correctamente registrados para poder solicitar préstamos |
| **Disparador (Trigger)** | El bibliotecario o administrador necesita registrar, modificar, eliminar o consultar la información de un lector |
| **Prioridad / Frecuencia** | Alta; uso frecuente (altas y actualizaciones habituales de lectores) |
| **Reglas de negocio relacionadas** | RN-08 (DNI o legajo único); RN-09 (no eliminar lector con préstamos activos) |

---

### 1. BREVE DESCRIPCIÓN
Permite registrar, modificar, eliminar y consultar datos de lectores o alumnos para
mantener actualizada la información de quienes solicitan préstamos.

### 2. PRECONDICIONES
- El usuario debe haber iniciado sesión con un estado de autenticación activo (Token
  válido).
- El usuario debe tener permisos para gestionar lectores (rol Bibliotecario o
  Administrador).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201)
1. El usuario ingresa a la gestión de lectores: el Actor envía una petición
   `GET /api/lectores` a la **Capa de Presentación**.
2. El Sistema consulta la **Capa de Persistencia** y muestra el listado de lectores
   registrados con código **HTTP 200**.
3. El usuario selecciona una acción: registrar (`POST /api/lectores`), modificar
   (`PUT /api/lectores/{id}`), eliminar (`DELETE /api/lectores/{id}`) o consultar un
   lector puntual (`GET /api/lectores/{id}`).
4. La **Capa de Presentación** solicita o muestra los datos correspondientes a la
   acción elegida y valida que el formato de los datos (esquema) sea correcto.
5. El usuario confirma la operación.
6. La **Capa de Negocio** valida la información ingresada y verifica las reglas de
   negocio aplicables (RN-08).
7. El Sistema persiste los cambios en la **Capa de Persistencia** (tabla `Lectores`) y
   devuelve un código **HTTP 201** (alta) o **200** (modificación/baja) con la
   información resultante.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Registrar lector (HTTP 201 Created):**
  1. El usuario selecciona la opción de registrar lector y envía `POST /api/lectores`
     con nombre, apellido, mail y DNI o legajo.
  2. La Capa de Negocio valida la información y verifica **RN-08**.
  3. El Sistema registra el lector y devuelve **HTTP 201 Created** con los datos del
     nuevo lector.

* **3b. Modificar lector (HTTP 200 OK):**
  1. El usuario selecciona un lector existente y envía `PUT /api/lectores/{id}` con los
     datos a modificar.
  2. La Capa de Negocio valida y verifica los cambios conforme a **RN-08**.
  3. El Sistema actualiza el registro y devuelve **HTTP 200 OK** con los datos
     actualizados.

* **3c. Eliminar lector (HTTP 200 OK):**
  1. El usuario selecciona un lector existente y envía `DELETE /api/lectores/{id}`.
  2. La Capa de Negocio verifica si el lector posee préstamos activos, conforme a
     **RN-09**.
  3. Si no posee préstamos activos, el Sistema elimina o desactiva el registro y
     devuelve **HTTP 200 OK**.

* **3c-1. Lector con préstamos activos (HTTP 409 Conflict):**
  1. Si en el Paso 2 del flujo 3c el lector posee préstamos activos.
  2. La Capa de Negocio impide la eliminación del registro, conforme a **RN-09**.
  3. El Sistema devuelve un código **409 Conflict** indicando que el lector no puede
     eliminarse mientras tenga préstamos activos. Fin del caso de uso.

* **4a. Datos obligatorios incompletos (HTTP 400 Bad Request):**
  1. Si en el Paso 4 el Sistema detecta que faltan datos obligatorios (nombre, apellido,
     mail o DNI/legajo).
  2. La Capa de Presentación rechaza la petición por error de validación.
  3. El Sistema devuelve un código **400 Bad Request** informando los campos
     requeridos. Fin del caso de uso.

* **6a. DNI o legajo repetido (HTTP 409 Conflict):**
  1. Si en el Paso 6 el Sistema detecta que ya existe un lector registrado con el mismo
     DNI o legajo, violando **RN-08**.
  2. La Capa de Negocio rechaza la operación.
  3. El Sistema devuelve un código **409 Conflict** indicando que el dato debe ser
     único, permitiendo al usuario corregir la información. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No se identifican variaciones de datos o mecanismo adicionales a las descriptas en el
flujo principal y alternativo._

### 6. POSTCONDICIONES
- Los datos del lector quedan actualizados en el sistema.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta del listado/detalle, modificación o baja exitosa de un lector. |
| `201` | Created | Confirmación de alta de un nuevo lector. |
| `400` | Bad Request | Datos obligatorios faltantes en el alta o modificación del lector. |
| `409` | Conflict | DNI/legajo duplicado (RN-08) o intento de eliminar un lector con préstamos activos (RN-09). |

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario | Test integración |
| --- | --- | --- | --- |
| 3a. Registrar lector | `201 Created` | `CreateLectorAsync_WithValidData_SavesAndReturnsCreatedLector` | `CreateLector_ReturnsSuccessAndCreatedLector` |
| 3b. Modificar lector | `200 OK` | — (pendiente) | — (pendiente) |
| 3c. Eliminar lector | `200 OK` | `DeleteLectorAsync_WithoutActiveLoans_DeletesLector` | — (pendiente) |
| 3c-1. Lector con préstamos activos | `409 Conflict` | `DeleteLectorAsync_WhenLectorHasActiveLoans_ThrowsConflictException` | — (pendiente) |
| 4a. Datos obligatorios incompletos | `400 Bad Request` | `CreateLectorAsync_WithMissingRequiredField_ThrowsValidationException` | `CreateLector_WithMissingRequiredField_Returns400BadRequest`, `CreateLector_WithInvalidEmail_Returns400BadRequest` |
| 6a. DNI o legajo repetido | `409 Conflict` | `CreateLectorAsync_WhenIdentificadorAlreadyExists_ThrowsConflictException` | `CreateLector_WhenDuplicateIdentificador_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Las filas
> marcadas como "pendiente" todavía no tienen test dedicado; quedan para una
> iteración futura.
