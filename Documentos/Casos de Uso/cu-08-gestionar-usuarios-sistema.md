# Caso de Uso: Gestionar Usuarios del Sistema

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso_Limpio.docx`.
> Reglas de negocio RN-19 (solo el administrador gestiona usuarios), RN-20 (email,
> contraseña y rol obligatorios) y RN-21 (contraseñas de mínimo 6 caracteres,
> almacenadas de forma segura) **implementadas**: RN-19 con
> `[Authorize(Roles = "Administrador")]` en `UsuariosController`, RN-20 y RN-21 en
> `UsuarioService` (hash PBKDF2 vía `IPasswordHasher`). La baja es lógica
> (`Activo = false`). La matriz de trazabilidad lista los tests reales.

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
   petición `GET /api/usuarios` a la **Capa de Presentación**.
2. El Sistema consulta la **Capa de Persistencia** y muestra los usuarios registrados
   con código **HTTP 200**.
3. El administrador selecciona una acción: registrar (`POST /api/usuarios`), modificar
   (`PUT /api/usuarios/{id}`), eliminar (`DELETE /api/usuarios/{id}`) o consultar un
   usuario puntual (`GET /api/usuarios/{id}`).
4. La **Capa de Presentación** solicita o muestra los datos correspondientes a la
   acción elegida y valida que el formato de los datos (esquema) sea correcto.
5. El administrador confirma la operación.
6. La **Capa de Negocio** valida la información ingresada y verifica las reglas de
   negocio aplicables (RN-20, RN-21).
7. El Sistema persiste los cambios en la **Capa de Persistencia** (tabla `Usuarios`) y
   devuelve un código **HTTP 201** (alta) o **200** (modificación/baja) con la
   información resultante.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Registrar usuario (HTTP 201 Created):**
  1. El administrador selecciona la opción de registrar usuario del sistema y envía
     `POST /api/usuarios` con email, contraseña y rol.
  2. La Capa de Negocio valida la información conforme a **RN-20** y **RN-21**.
  3. El Sistema registra el usuario y devuelve **HTTP 201 Created** con los datos del
     nuevo usuario.

* **3b. Modificar usuario (HTTP 200 OK):**
  1. El administrador selecciona un usuario existente y envía `PUT /api/usuarios/{id}`
     con el email, la contraseña o el rol a modificar.
  2. La Capa de Negocio valida los cambios conforme a **RN-20** y **RN-21**.
  3. El Sistema actualiza el registro y devuelve **HTTP 200 OK** con los datos
     actualizados.

* **3c. Eliminar usuario (HTTP 200 OK):**
  1. El administrador selecciona un usuario existente y envía
     `DELETE /api/usuarios/{id}`.
  2. El Sistema solicita confirmación.
  3. El administrador confirma la eliminación.
  4. El Sistema desactiva el usuario (`Activo = false`, baja lógica: ya no puede
     iniciar sesión) y devuelve **HTTP 200 OK**.

* **4a. Datos obligatorios incompletos o inválidos (HTTP 400 Bad Request):**
  1. Si el Sistema detecta que faltan datos obligatorios (email, contraseña o rol),
     que la contraseña tiene menos de 6 caracteres o que el rol no existe.
  2. La Capa de Negocio rechaza la petición (`UsuarioInvalidoException`).
  3. El Sistema devuelve un código **400 Bad Request** informando los campos
     requeridos. Fin del caso de uso.

* **6a. Email repetido (HTTP 409 Conflict):**
  1. Si en el Paso 6 el Sistema detecta que ya existe un usuario registrado con el
     mismo email.
  2. La Capa de Negocio rechaza la operación.
  3. El Sistema devuelve un código **409 Conflict** indicando que el email debe ser
     único, permitiendo corregir el dato. Fin del caso de uso.

* **Id inexistente (HTTP 404 Not Found):**
  1. Si al consultar, modificar o eliminar no existe un usuario con ese Id.
  2. El Sistema devuelve **404 Not Found**. Fin del caso de uso.

* **Sin permisos (HTTP 401 / 403):**
  1. Sin token válido el Sistema devuelve **401 Unauthorized**.
  2. Con token de Bibliotecario devuelve **403 Forbidden** (RN-19). Fin del caso de uso.

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
| `200` | OK | Consulta del listado/detalle, modificación o baja exitosa de un usuario. |
| `201` | Created | Confirmación de alta de un nuevo usuario del sistema. |
| `400` | Bad Request | Datos obligatorios faltantes, contraseña de menos de 6 caracteres o rol inexistente (RN-20, RN-21). |
| `401` | Unauthorized | Petición sin token válido. |
| `403` | Forbidden | Usuario autenticado que no es Administrador (RN-19). |
| `404` | Not Found | No existe un usuario con el Id indicado. |
| `409` | Conflict | Email duplicado (RN-20). |

### Matriz de trazabilidad CU-08 → Test

| Paso del CU | Código HTTP | Test unitario | Test de integración |
| --- | --- | --- | --- |
| 3a. Registrar usuario | `201 Created` | `CreateUsuarioAsync_WithValidData_SavesAndReturnsCreatedUsuario` | `CreateUsuario_ReturnsSuccessAndCreatedUsuario` |
| 3b. Modificar usuario | `200 OK` | `UpdateUsuarioAsync_WithValidData_UpdatesUsuario`, `UpdateUsuarioAsync_WithNewPassword_HashesPassword` | `UpdateUsuario_ReturnsSuccessAndUpdatedUsuario` |
| 3c. Eliminar usuario | `200 OK` | `DeleteUsuarioAsync_WithConfirmation_DeletesUsuario` | `DeleteUsuario_ReturnsSuccessAndDeletesUsuario` |
| 4a. Datos obligatorios incompletos o inválidos | `400 Bad Request` | `CreateUsuarioAsync_WithMissingEmail_ThrowsValidationException`, `CreateUsuarioAsync_WithShortPassword_ThrowsValidationException`, `CreateUsuarioAsync_WithInvalidRol_ThrowsValidationException` | `CreateUsuario_WithMissingRequiredField_Returns400BadRequest` |
| 6a. Email repetido | `409 Conflict` | `CreateUsuarioAsync_WhenEmailAlreadyExists_ThrowsConflictException`, `UpdateUsuarioAsync_WhenEmailBelongsToOtherUsuario_ThrowsConflictException` | `CreateUsuario_WhenDuplicateEmail_Returns409Conflict` |
| Id inexistente | `404 Not Found` | `GetUsuarioByIdAsync_WithUnknownId_ThrowsNotFoundException`, `UpdateUsuarioAsync_WithUnknownId_ThrowsNotFoundException`, `DeleteUsuarioAsync_WithUnknownId_ThrowsNotFoundException` | `GetUsuario_WithUnknownId_Returns404NotFound` |
| RN-19 sin permisos | `401` / `403` | — (lo resuelve `[Authorize(Roles)]` en Presentación) | `GetUsuarios_WithoutToken_Returns401Unauthorized`, `GetUsuarios_AsBibliotecario_Returns403Forbidden` |
