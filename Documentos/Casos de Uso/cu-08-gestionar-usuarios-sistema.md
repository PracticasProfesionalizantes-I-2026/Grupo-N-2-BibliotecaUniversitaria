# Caso de Uso: Gestionar Usuarios del Sistema

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso_Limpio.docx`.
> Reglas de negocio RN-20 (email, contraseña y rol obligatorios) y RN-21
> (contraseñas de mínimo 6 caracteres, almacenadas con hash vía
> `PasswordHasher<Usuario>`) **implementadas** en `UsuarioService`. RN-19 (solo el
> administrador gestiona usuarios) queda **pendiente**: depende de autenticación
> JWT y `[Authorize]` por rol, fuera de alcance de esta rama (ver CU-01, Rama 3).
> La matriz de trazabilidad de más abajo refleja los tests que existen realmente
> en `tests/BiblioGest.UnitTests/UsuarioServiceTests.cs` y
> `tests/BiblioGest.IntegrationTests/UsuariosControllerTests.cs`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-08 |
| **Nombre** | Gestionar Usuarios del Sistema |
| **Actor Principal** | Administrador |
| **Actores Secundarios** | Bibliotecario |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administrador → controlar el acceso del personal autorizado al sistema; Bibliotecario → contar con una cuenta activa y con el rol correcto para operar |
| **Disparador (Trigger)** | El administrador necesita administrar el acceso del personal al sistema |
| **Prioridad / Frecuencia** | Media; frecuencia periódica (altas y bajas de personal, no en cada operación) |
| **Reglas de negocio relacionadas** | RN-19 (solo el administrador puede gestionar usuarios del sistema); RN-20 (cada usuario debe tener email, contraseña y rol); RN-21 (contraseñas de mínimo 6 caracteres, almacenamiento seguro) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador registrar, modificar, eliminar y consultar usuarios del
sistema, como bibliotecarios o personal autorizado.

### 2. PRECONDICIONES
- El administrador debe haber iniciado sesión con un estado de autenticación activo
  (Token válido).
- El administrador debe tener permisos para gestionar usuarios del sistema, conforme
  a **RN-19**.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200/201)
1. El administrador ingresa a la gestión de usuarios del sistema: el Actor envía una
   petición `GET /api/v1/usuarios` a la **Capa de Presentación**.
2. El Sistema consulta la **Capa de Persistencia** y muestra los usuarios registrados
   con código **HTTP 200**.
3. El administrador selecciona una acción: registrar (`POST /api/v1/usuarios`), modificar
   (`PUT /api/v1/usuarios/{id}`), eliminar (`DELETE /api/v1/usuarios/{id}`) o consultar un
   usuario puntual (`GET /api/v1/usuarios/{id}`).
4. La **Capa de Presentación** solicita o muestra los datos correspondientes a la
   acción elegida y valida que el formato de los datos (esquema) sea correcto.
5. El administrador confirma la operación.
6. La **Capa de Negocio** valida la información ingresada y verifica las reglas de
   negocio aplicables (RN-20, RN-21).
7. El Sistema persiste los cambios en la **Capa de Persistencia** (tabla `Usuarios`) y
   devuelve un código **HTTP 201** (alta), **200** (modificación) o **204**
   (baja lógica) con la información resultante.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Registrar usuario (HTTP 201 Created):**
  1. El administrador selecciona la opción de registrar usuario del sistema y envía
     `POST /api/v1/usuarios` con email, contraseña y rol.
  2. La Capa de Negocio valida la información conforme a **RN-20** y **RN-21**.
  3. El Sistema registra el usuario y devuelve **HTTP 201 Created** con los datos del
     nuevo usuario.

* **3b. Modificar usuario (HTTP 200 OK):**
  1. El administrador selecciona un usuario existente y envía `PUT /api/v1/usuarios/{id}`
     con el email, la contraseña o el rol a modificar.
  2. La Capa de Negocio valida los cambios conforme a **RN-20** y **RN-21**.
  3. El Sistema actualiza el registro y devuelve **HTTP 200 OK** con los datos
     actualizados.

* **3c. Eliminar usuario (HTTP 204 No Content):**
  1. El administrador selecciona un usuario existente y envía
     `DELETE /api/v1/usuarios/{id}`.
  2. El Sistema solicita confirmación.
  3. El administrador confirma la eliminación.
  4. El Sistema desactiva el usuario (baja lógica, `Activo = false`) y devuelve
     **HTTP 204 No Content**.

* **4a. Datos obligatorios incompletos (HTTP 400 Bad Request):**
  1. Si en el Paso 4 el Sistema detecta que faltan datos obligatorios (email,
     contraseña o rol).
  2. La Capa de Presentación rechaza la petición por error de validación.
  3. El Sistema devuelve un código **400 Bad Request** informando los campos
     requeridos. Fin del caso de uso.

* **6a. Email repetido (HTTP 409 Conflict):**
  1. Si en el Paso 6 el Sistema detecta que ya existe un usuario registrado con el
     mismo email.
  2. La Capa de Negocio rechaza la operación.
  3. El Sistema devuelve un código **409 Conflict** indicando que el email debe ser
     único, permitiendo corregir el dato. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No se identifican variaciones de datos o mecanismo adicionales a las descriptas en el
flujo principal y alternativo._

### 6. POSTCONDICIONES
- Los usuarios del sistema quedan actualizados.
- Los roles y permisos quedan configurados según corresponda.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta del listado/detalle o modificación exitosa de un usuario. |
| `201` | Created | Confirmación de alta de un nuevo usuario del sistema. |
| `204` | No Content | Baja lógica exitosa de un usuario (`Activo = false`). |
| `400` | Bad Request | Datos obligatorios faltantes o inválidos (nombre, email, rol o contraseña con RN-21). |
| `404` | Not Found | El usuario indicado no existe. |
| `409` | Conflict | Email duplicado (RN-20). |

### Matriz de trazabilidad CU-08 → Test

| Paso del CU | Excepción / Código | Test unitario | Test integración |
| --- | --- | --- | --- |
| 3a. Registrar usuario | `201 Created` | `CreateUsuarioAsync_WithValidData_SavesAndReturnsCreatedUsuario` | `CreateUsuario_ReturnsSuccessAndCreatedUsuario`, `CreateUsuario_DoesNotExposePassword` |
| 3b. Modificar usuario | `200 OK` | `UpdateUsuarioAsync_WithValidData_UpdatesUsuario` | `UpdateUsuario_ReturnsSuccessAndUpdatedUsuario` |
| 3c. Eliminar usuario | `204 No Content` (baja lógica) | `DeleteUsuarioAsync_WithConfirmation_DeletesUsuario` | `DeleteUsuario_ReturnsSuccessAndDeactivatesUsuario` |
| 4a. Datos obligatorios incompletos | `400 Bad Request` | `CreateUsuarioAsync_WithMissingRequiredField_ThrowsValidationException` | `CreateUsuario_WithMissingRequiredField_Returns400BadRequest`, `CreateUsuario_WithInvalidRol_Returns400BadRequest` |
| 6a. Email repetido | `409 Conflict` | `CreateUsuarioAsync_WhenEmailAlreadyExists_ThrowsConflictException` | `CreateUsuario_WhenDuplicateEmail_Returns409Conflict` |
| RN-21. Contraseña corta | `400 Bad Request` | `CreateUsuarioAsync_WithShortPassword_ThrowsValidationException` | `CreateUsuario_WithShortPassword_Returns400BadRequest` |
| Usuario inexistente | `404 Not Found` | `GetUsuarioByIdAsync_WhenUsuarioDoesNotExist_ThrowsNotFoundException` | `GetUsuario_WithUnknownId_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso tiene al menos un test unitario y uno de
> integración. RN-19 (restricción por rol Administrador) queda fuera de esta matriz
> hasta que se implemente la autenticación (CU-01, Rama 3).
