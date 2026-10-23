# Caso de Uso: Gestionar Lectores

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso.pdf`.
> Reglas de negocio RN-08 (unicidad de DNI/legajo) y RN-09 (no eliminar lector con
> préstamos activos) **implementadas** en `LectorService`. El DNI o legajo se guarda en
> un único campo `Identificador`. Requiere token JWT (`[Authorize]`). La matriz de
> trazabilidad lista los tests reales.
> **Actualizado:** RN-08 ahora también valida el formato del email (único,
> se guarda en minúsculas, `LectorEmailDuplicadoException` si está
> repetido) y largos máximos por campo; el identificador se normaliza a
> mayúsculas sin espacios para que la unicidad no distinga mayúsculas. El
> flujo 3c-1 (eliminar lector) se endureció igual que en CU-02: se rechaza
> si el lector tiene **cualquier préstamo en su historial** (activo o ya
> devuelto), no solo préstamos activos (ver `Documentos/Trazabilidad.md`).

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
  2. La Capa de Negocio verifica si el lector posee préstamos en su historial
     (activos o ya devueltos), conforme a **RN-09**.
  3. Si no posee ningún préstamo registrado, el Sistema elimina el registro y
     devuelve **HTTP 200 OK**.

* **3c-1. Lector con préstamos en su historial (HTTP 409 Conflict):**
  1. Si en el Paso 2 del flujo 3c el lector posee algún préstamo registrado,
     esté activo o ya devuelto.
  2. La Capa de Negocio impide la eliminación del registro
     (`LectorConHistorialPrestamosException`), conforme a **RN-09**, para no
     perder el historial de movimientos.
  3. El Sistema devuelve un código **409 Conflict** indicando que el lector no puede
     eliminarse mientras tenga préstamos en su historial. Fin del caso de uso.

* **4a. Datos obligatorios incompletos (HTTP 400 Bad Request):**
  1. Si en el Paso 4 el Sistema detecta que faltan datos obligatorios (nombre, apellido,
     mail o DNI/legajo).
  2. La Capa de Negocio rechaza la petición por error de validación.
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
| `401` | Unauthorized | Petición sin token válido. |
| `404` | Not Found | No existe un lector con el Id indicado. |
| `409` | Conflict | DNI/legajo duplicado (RN-08), email de lector duplicado, o intento de eliminar un lector con préstamos en su historial (RN-09). |

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Código HTTP | Test unitario | Test de integración |
| --- | --- | --- | --- |
| 3a. Registrar lector | `201 Created` | `CreateLectorAsync_WithValidData_SavesAndReturnsCreatedLector` | `CreateLector_ReturnsSuccessAndCreatedLector` |
| 3b. Modificar lector | `200 OK` | `UpdateLectorAsync_WithValidData_UpdatesLector` | `UpdateLector_ReturnsSuccessAndUpdatedLector` |
| 3c. Eliminar lector | `200 OK` | `DeleteLectorAsync_WithoutLoanHistory_DeletesLector` | `DeleteLector_WithoutActiveLoans_Returns200OK` |
| 3c-1. Lector con préstamos en su historial | `409 Conflict` | `DeleteLectorAsync_WhenLectorHasLoanHistory_ThrowsConflictException` | `DeleteLector_WithActiveLoans_Returns409Conflict`, `DeleteLector_AfterLoanWasReturned_Returns409ConflictAndKeepsHistory` |
| 4a. Datos obligatorios incompletos | `400 Bad Request` | `CreateLectorAsync_WithMissingOrBlankField_ThrowsValidationException` | `CreateLector_WithMissingRequiredField_Returns400BadRequest` |
| 6a. DNI o legajo repetido | `409 Conflict` | `CreateLectorAsync_WhenIdentificadorAlreadyExists_ThrowsConflictException`, `CreateLectorAsync_WhenIdentificadorDuplicadoWithDifferentCaseAndSpaces_ThrowsConflictException`, `UpdateLectorAsync_WhenIdentificadorBelongsToOtherLector_ThrowsConflictException` | `CreateLector_WhenDuplicateIdentificador_Returns409Conflict`, `CreateLector_WithDuplicateIdentificadorDifferentCaseAndSpaces_Returns409Conflict` |
| Email inválido (nuevo) | `400 Bad Request` | `CreateLectorAsync_WithInvalidEmailFormat_ThrowsValidationException` | `CreateLector_WithInvalidEmail_Returns400BadRequest` |
| Email duplicado (nuevo) | `409 Conflict` | `CreateLectorAsync_WhenEmailAlreadyExists_ThrowsConflictException` | `CreateLector_WithDuplicateEmailDifferentCaseAndSpaces_Returns409Conflict` |
| Texto demasiado largo (nuevo) | `400 Bad Request` | `CreateLectorAsync_WithTooLongTitulo_ThrowsValidationException` | `CreateLector_WithTooLongNombre_Returns400BadRequest` |
| Id inexistente | `404 Not Found` | `GetLectorByIdAsync_WhenLectorDoesNotExist_ThrowsNotFoundException` | `GetLector_WithUnknownId_Returns404NotFound` |
