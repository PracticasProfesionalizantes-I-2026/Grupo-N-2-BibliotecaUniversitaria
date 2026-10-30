# Caso de Uso: Consultar Disponibilidad de Libros

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso_Limpio.docx`.
> Reglas de negocio RN-06 (disponibilidad según stock actual) y RN-07 (libro sin stock
> no puede prestarse) **implementadas**: RN-06 con el campo calculado `Disponible`
> (`Stock > 0`) en `LibroService`, RN-07 en `PrestamoService.CreateAsync` (ver CU-05,
> alternativa 5a). La matriz de trazabilidad lista los tests reales.
> **Actualizado:** además de `disponible` (booleano), la respuesta incluye
> `disponibilidadTexto`, un mensaje ya armado por `LibroService` (por
> ejemplo "Disponible (3 ejemplares)" o "Sin stock") para que el frontend
> no tenga que construirlo.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Consultar Disponibilidad de Libros |
| **Actor Principal** | Bibliotecario / Administrador |
| **Actores Secundarios** | Lector / Alumno |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Bibliotecario/Administrador → informar con precisión la disponibilidad de un libro; Lector → conocer si un libro puede solicitarse en préstamo |
| **Disparador (Trigger)** | Un lector solicita información sobre un libro o el usuario del sistema necesita consultar su disponibilidad |
| **Prioridad / Frecuencia** | Alta; uso muy frecuente (consulta habitual previa a un préstamo) |
| **Reglas de negocio relacionadas** | RN-06 (disponibilidad según stock actual); RN-07 (libro sin stock no puede ser prestado) |

---

### 1. BREVE DESCRIPCIÓN
Permite verificar si un libro se encuentra disponible para préstamo, consultando su
información y stock actual.

### 2. PRECONDICIONES
- El usuario debe haber iniciado sesión con un estado de autenticación activo (Token
  válido).
- El libro debe estar registrado para poder consultar su disponibilidad.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El lector solicita información sobre un libro al bibliotecario, o el usuario del
   sistema inicia la consulta: el Actor envía una petición
   `GET /api/libros?busqueda={texto}` a la **Capa de Presentación**.
2. El Sistema busca el libro por título, autor o ISBN en la **Capa de Persistencia**.
3. El Sistema muestra los resultados encontrados con código **HTTP 200 OK**.
4. El usuario selecciona el libro correspondiente (`GET /api/libros/{id}`).
5. El Sistema muestra los datos del libro y su stock disponible con código
   **HTTP 200 OK**, conforme a **RN-06**.
6. El usuario informa la disponibilidad al lector, si corresponde.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Libro no encontrado (HTTP 200 OK con listado vacío):**
  1. Si en el Paso 2 el Sistema no encuentra coincidencias con la búsqueda realizada.
  2. El Sistema devuelve un código **200 OK** con un listado vacío, informando que no
     existen resultados.
  3. El Sistema permite realizar una nueva búsqueda. Fin del caso de uso.

* **5a. Stock agotado (HTTP 200 OK):**
  1. Si en el Paso 5 el Sistema detecta que el stock disponible es igual a cero,
     conforme a **RN-07**.
  2. El Sistema informa, dentro de la misma respuesta **200 OK**, que el libro no se
     encuentra disponible para préstamo. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No se identifican variaciones de datos o mecanismo adicionales a las descriptas en el
flujo principal y alternativo._

### 6. POSTCONDICIONES
- El usuario conoce el estado de disponibilidad del libro.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta exitosa de resultados (incluyendo listado vacío o stock agotado). |
| `401` | Unauthorized | Petición sin token válido. |

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Código HTTP | Test unitario | Test de integración |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `SearchLibrosAsync_WithText_ReturnsMatchingLibrosWithDisponibilidad` | `SearchLibros_WithText_ReturnsMatchingLibros` |
| 2a. Libro no encontrado | `200 OK` | `SearchLibrosAsync_WithNoMatches_ReturnsEmptyList` | `SearchLibros_WithNoMatches_Returns200OKWithEmptyList` |
| 5a. Stock agotado | `200 OK` | `GetLibroDisponibilidadAsync_WithZeroStock_ReturnsUnavailable` | `GetLibroDisponibilidad_WithZeroStock_Returns200OKUnavailable` |
