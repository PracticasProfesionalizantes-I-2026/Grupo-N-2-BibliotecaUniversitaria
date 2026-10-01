# Caso de Uso: Controlar Mora o Retrasos

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3), a partir del documento
> `BiblioGest_CasosDeUso.pdf`.
> Regla de negocio RN-14 (identificación de préstamos vencidos) **implementada** en
> `PrestamoRepository.GetVencidosAsync` / `LectorTieneMoraAsync`; RN-16 (sin multas
> automáticas) se cumple por diseño, al no existir esa funcionalidad en el MVP. La
> matriz de trazabilidad de más abajo refleja los tests que existen realmente en
> `tests/BiblioGest.UnitTests/PrestamoServiceTests.cs` y
> `tests/BiblioGest.IntegrationTests/PrestamosControllerTests.cs`; las filas sin test
> dedicado quedan marcadas como pendientes.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Controlar Mora o Retrasos |
| **Actor Principal** | Bibliotecario |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Bibliotecario → identificar rápidamente los préstamos vencidos para realizar el seguimiento correspondiente; Administración → contar con visibilidad sobre el cumplimiento de los plazos de devolución; Lector → ser advertido y gestionado ante un atraso en la devolución de un libro |
| **Disparador (Trigger)** | El bibliotecario necesita revisar préstamos atrasados |
| **Prioridad / Frecuencia** | Media; frecuencia periódica (revisión habitual, no en cada operación) |
| **Reglas de negocio relacionadas** | RN-14 (identificación de préstamos vencidos); RN-15 (el control de mora corresponde al bibliotecario); RN-16 (sin sistema de multas automáticas en el MVP) |

---

### 1. BREVE DESCRIPCIÓN
Permite identificar préstamos vencidos y lectores con devoluciones fuera de término
para que el bibliotecario pueda realizar el seguimiento correspondiente.

### 2. PRECONDICIONES
- El bibliotecario debe haber iniciado sesión con un estado de autenticación activo
  (Token válido), conforme a **RN-15**.
- Deben existir préstamos registrados en el sistema.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El bibliotecario ingresa a la sección de mora o préstamos vencidos: el Actor envía
   una petición `GET /api/prestamos/mora` a la **Capa de Presentación**.
2. El Sistema consulta los préstamos registrados en la **Capa de Persistencia**.
3. La **Capa de Negocio** identifica aquellos cuya fecha de vencimiento ya pasó y no
   fueron devueltos, conforme a **RN-14**.
4. El Sistema lista los préstamos vencidos con código **HTTP 200 OK**.
5. El bibliotecario consulta el detalle del lector y del libro pendiente
   (`GET /api/prestamos/{id}`).
6. El bibliotecario realiza el seguimiento correspondiente.
7. Si el lector devuelve el libro, el bibliotecario registra la devolución
   (`PUT /api/prestamos/{id}/devolucion`).
8. El Sistema actualiza el estado del préstamo y devuelve un código **HTTP 200 OK**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. No hay préstamos vencidos (HTTP 200 OK):**
  1. Si en el Paso 3 la Capa de Negocio no detecta préstamos atrasados.
  2. El Sistema devuelve un código **200 OK** con un listado vacío, informando que no
     existen moras o retrasos registrados. Fin del caso de uso.

* **7a. El lector no devuelve el libro:**
  1. Si en el Paso 7 el lector no realiza la devolución.
  2. El préstamo continúa marcado como vencido en la **Capa de Persistencia**.
  3. El Sistema mantiene visible el caso en el listado de mora (sin cambio de estado ni
     nueva respuesta HTTP; la restricción se hace efectiva en el próximo intento de
     préstamo del lector, ver CU-05 alternativa 3b). Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
_No se identifican variaciones de datos o mecanismo adicionales a las descriptas en el
flujo principal y alternativo._

### 6. POSTCONDICIONES
- El sistema mantiene actualizado el estado de los préstamos vencidos.
- El bibliotecario puede realizar seguimiento de los lectores con retrasos.
- **RN-16**: no se genera ninguna multa automática como consecuencia de este caso de
  uso (fuera del alcance del MVP).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta exitosa del listado de préstamos vencidos (incluyendo el caso de listado vacío) o confirmación de una devolución tardía registrada. |

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario | Test integración |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GetPrestamosVencidosAsync_ReturnsOverdueList` | `GetPrestamosVencidos_ReturnsSuccessAndOverdueList` |
| 2a. No hay préstamos vencidos | `200 OK` | — (pendiente) | — (pendiente) |
| 7a. El lector no devuelve el libro | — | — (pendiente) | *(cubierto indirectamente por CU-05, alternativa 3b — restricción de nuevo préstamo por mora)* |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Las filas
> marcadas como "pendiente" quedan para una iteración futura.
