# Caso de Uso: Iniciar Sesión

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso_Limpio.docx`.
> Este caso de uso está implementado: `AuthController` (API), `AuthService`
> (BusinessLogic) y `UsuarioRepository` (DataAccess). La regla RN-01 (usuarios
> registrados y activos) se valida en el login. La validación del largo mínimo de
> contraseña de RN-02 se aplica al crear/modificar usuarios (CU-08), no en el login.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Bibliotecario / Administrador |
| **Actores Secundarios** | No aplica |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Bibliotecario/Administrador → acceder de forma segura a las funcionalidades correspondientes a su rol; Administración → garantizar que solo personal autorizado opere el sistema |
| **Disparador (Trigger)** | El usuario solicita ingresar al sistema |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente (primer paso de cada sesión de trabajo) |
| **Reglas de negocio relacionadas** | RN-01 (solo usuarios registrados y activos); RN-02 (contraseñas de mínimo 6 caracteres, almacenamiento seguro) |

---

### 1. BREVE DESCRIPCIÓN
Permite que el personal autorizado ingrese al sistema para acceder a las
funcionalidades según su rol.

### 2. PRECONDICIONES
- El usuario debe estar registrado en el sistema.
- El usuario debe tener una cuenta activa.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El usuario accede al inicio de sesión: el Actor envía una petición
   `POST /api/auth/login` a la **Capa de Presentación** con usuario/email y
   contraseña.
2. La **Capa de Presentación** valida que el formato de los datos (esquema) sea
   correcto.
3. La **Capa de Negocio** valida las credenciales contra la **Capa de Persistencia**,
   conforme a **RN-01** y **RN-02**.
4. El Sistema genera el token de sesión y devuelve un código **HTTP 200 OK** con el
   token y el panel correspondiente al rol del usuario.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Credenciales incorrectas (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el usuario/email existe pero la
     contraseña no coincide.
  2. El Sistema informa que las credenciales son inválidas.
  3. El Sistema devuelve un código **401 Unauthorized**, permitiendo reintentar el
     ingreso. Fin del caso de uso.

* **3b. Usuario inexistente (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 la Capa de Negocio detecta que no existe una cuenta asociada al
     usuario/email ingresado.
  2. El Sistema informa que el usuario no se encuentra registrado (mismo código que
     3a, para no revelar cuál dato es incorrecto).
  3. El Sistema devuelve un código **401 Unauthorized** y regresa al inicio de sesión.
     Fin del caso de uso.

* **3c. Usuario inactivo (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 la Capa de Negocio detecta que el usuario/email existe y la
     contraseña coincide, pero la cuenta está inactiva (conforme a **RN-01**).
  2. El Sistema informa que las credenciales son inválidas (mismo código que 3a y 3b,
     para no revelar el estado de la cuenta).
  3. El Sistema devuelve un código **401 Unauthorized** y regresa al inicio de sesión.
     Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No se identifican variaciones de datos o mecanismo adicionales a las descriptas en el
flujo principal y alternativo._

### 6. POSTCONDICIONES
- El usuario queda autenticado dentro del sistema.
- El sistema habilita las funcionalidades correspondientes a su rol.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa; devuelve el token de sesión. |
| `400` | Bad Request | Formato de datos inválido (Paso 2): email vacío, email con formato incorrecto o contraseña vacía. |
| `401` | Unauthorized | Credenciales incorrectas (contraseña incorrecta) o usuario inexistente/inactivo (RN-01). |

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Código HTTP | Test unitario | Test de integración |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `LoginAsync_ConCredencialesValidas_DevuelveTokenYDatosDeUsuario` | `Login_WithValidAdminCredentials_ReturnsOkAndToken`, `Login_WithValidBibliotecarioCredentials_ReturnsOkAndToken` |
| 3a. Contraseña incorrecta | `401 Unauthorized` | `LoginAsync_ConPasswordIncorrecta_LanzaCredencialesInvalidasException` | `Login_WithWrongPassword_Returns401Unauthorized` |
| 3b. Usuario inexistente | `401 Unauthorized` | `LoginAsync_ConEmailInexistente_LanzaCredencialesInvalidasException` | `Login_WithUnknownEmail_Returns401Unauthorized` |
| 3c. Usuario inactivo | `401 Unauthorized` | `LoginAsync_ConUsuarioInactivo_LanzaCredencialesInvalidasException` | — |
| Validación de formato (Paso 2) | `400 Bad Request` | — | `Login_WithEmptyEmail_Returns400BadRequest`, `Login_WithInvalidEmailFormat_Returns400BadRequest`, `Login_WithEmptyPassword_Returns400BadRequest` |
