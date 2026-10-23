# Caso de Uso: Generar Reportes

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso_Limpio.docx`.
> Reglas de negocio RN-17 (reportes básicos en el MVP) y RN-18 (estadísticas avanzadas
> fuera de alcance) **implementadas** en `ReporteService`: tres reportes básicos
> (`resumen`, `prestamos` con filtro de fechas y `mora`), sin estadísticas avanzadas.
> Acceso con cualquier usuario autenticado (`[Authorize]`). La matriz de trazabilidad
> lista los tests reales.
> **Actualizado:** los préstamos que aparecen en los reportes `prestamos` y
> `mora` incluyen los mismos campos calculados que CU-05/CU-06
> (`estado`, `diasRestantes`, `diasDeAtraso`, `mensaje`), resueltos por
> `PrestamoMapper` con el `TimeProvider` del service — nunca por el
> frontend.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Generar Reportes |
| **Actor Principal** | Bibliotecario / Administrador |
| **Actores Secundarios** | No aplica |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Bibliotecario/Administrador → contar con información resumida para el control y seguimiento de la biblioteca; Administración → tomar decisiones basadas en datos de uso del sistema |
| **Disparador (Trigger)** | El usuario necesita obtener información resumida para control o seguimiento |
| **Prioridad / Frecuencia** | Media; frecuencia periódica (consulta habitual, no en cada operación) |
| **Reglas de negocio relacionadas** | RN-17 (reportes del MVP deben ser básicos); RN-18 (gestión avanzada de reportes o estadísticas fuera del alcance inicial) |

---

### 1. BREVE DESCRIPCIÓN
Permite consultar información general sobre libros, préstamos, lectores y actividad
del sistema para facilitar el control y seguimiento de la biblioteca.

### 2. PRECONDICIONES
- El usuario debe haber iniciado sesión con un estado de autenticación activo (Token
  válido).
- Deben existir datos registrados en el sistema.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El usuario ingresa a la sección de reportes: el Actor envía una petición
   `GET /api/reportes` a la **Capa de Presentación**, que muestra las opciones de
   reportes disponibles.
2. El usuario selecciona el tipo de reporte que desea consultar
   (`GET /api/reportes/{tipo}`).
3. La **Capa de Presentación** solicita filtros si corresponde y valida su formato.
4. El usuario confirma la consulta.
5. La **Capa de Negocio** procesa la información registrada en la **Capa de
   Persistencia**, conforme a **RN-17**.
6. El Sistema devuelve el reporte solicitado con código **HTTP 200 OK**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. No existen datos suficientes (HTTP 200 OK con resultado vacío):**
  1. Si en el Paso 5 la Capa de Negocio detecta que no hay información registrada para
     generar el reporte.
  2. El Sistema devuelve un código **200 OK** con el reporte vacío, informando que no
     hay datos disponibles. Fin del caso de uso.

* **3a. Filtro inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 3 el Sistema detecta que el filtro ingresado no es válido.
  2. La Capa de Negocio rechaza la petición (`ReporteInvalidoException`); también se
     rechaza un tipo de reporte inexistente.
  3. El Sistema devuelve un código **400 Bad Request**, solicitando corregir los
     criterios de búsqueda. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No se identifican variaciones de datos o mecanismo adicionales a las descriptas en el
flujo principal y alternativo._

### 6. POSTCONDICIONES
- El usuario visualiza información útil para la toma de decisiones.
- **RN-18**: el reporte generado se limita al alcance básico del MVP, sin
  funcionalidades estadísticas avanzadas.

---

## Anexo: matrices de referencia

### Reportes disponibles

| Tipo | Ruta | Contenido | Filtros |
| --- | --- | --- | --- |
| — | `GET /api/reportes` | Lista de reportes disponibles | — |
| `resumen` | `GET /api/reportes/resumen` | Total de libros, libros sin stock, total de lectores y préstamos activos/devueltos/vencidos | — |
| `prestamos` | `GET /api/reportes/prestamos?desde=&hasta=` | Préstamos realizados en el rango (por fecha de préstamo, ambos días incluidos) | `desde`, `hasta` (opcionales) |
| `mora` | `GET /api/reportes/mora` | Préstamos activos vencidos | — |

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Reporte generado con éxito (incluyendo el caso de resultado vacío, con `mensaje` "No hay datos disponibles para el reporte."). |
| `400` | Bad Request | Filtro inválido (`desde` posterior a `hasta`, o fecha con formato inválido) o tipo de reporte inexistente. |
| `401` | Unauthorized | Petición sin token válido. |

### Matriz de trazabilidad CU-07 → Test

| Paso del CU | Código HTTP | Test unitario | Test de integración |
| --- | --- | --- | --- |
| Paso 1. Opciones de reportes | `200 OK` | `GetTipos_ReturnsReportesDisponibles` | `GetTipos_ReturnsReportesDisponibles` |
| Flujo principal | `200 OK` | `GetReporteAsync_WithValidType_ReturnsReportData`, `GetReporteAsync_Prestamos_WithRange_PassesDatesToRepository` | `GetReporte_ReturnsSuccessAndData`, `GetReporte_Mora_ReturnsPrestamosVencidos` |
| 5a. No existen datos suficientes | `200 OK` | `GetReporteAsync_WithNoData_ReturnsEmptyReport` | `GetReporte_WithNoData_Returns200OKEmpty` |
| 3a. Filtro inválido | `400 Bad Request` | `GetReporteAsync_WithInvalidFilter_ThrowsValidationException`, `GetReporteAsync_WithUnknownType_ThrowsValidationException` | `GetReporte_WithInvalidFilter_Returns400BadRequest`, `GetReporte_WithUnknownType_Returns400BadRequest` |
| Sin autenticación | `401 Unauthorized` | — (lo resuelve `[Authorize]` en Presentación) | `GetReporte_WithoutToken_Returns401Unauthorized` |
