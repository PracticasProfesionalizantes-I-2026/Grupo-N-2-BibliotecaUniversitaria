# Resumen de Evaluación del Proyecto: BiblioGest API

**Evaluadores:** Arquitecto de Software Senior, Analista Funcional y Auditor QA  
**Proyecto:** BiblioGest — Sistema de Gestión de Biblioteca Universitaria (Práctica Profesionalizante I, Grupo N°2)  
**Alumnos:** Lautaro Navarro y Elías Korell  
**Fecha de Evaluación:** Noviembre 2026  

---

## 1. Visión General del Proyecto y Madurez del SDLC

El proyecto **BiblioGest** presenta un nivel de madurez y rigor en el Ciclo de Vida del Desarrollo de Software (SDLC) **sobresaliente y marcadamente superior al promedio académico habitual**. 

Se observa un proceso estructurado de ingeniería de software que abarca:
1. **Fase de Análisis y Especificación:** Definición detallada de 8 Casos de Uso (CU-01 a CU-08) complementados con una matriz de reglas de negocio explícitas (RN-01 a RN-21).
2. **Fase de Diseño y Arquitectura:** Implementación de un modelo N-Tier estricto (`API / Controller → BusinessLogic / Service → DataAccess / Repository → DbContext`) en .NET 10, con separación física y lógica por proyectos (.csproj).
3. **Fase de Implementación y Trazabilidad:** Cobertura de las 4 complejidades avanzadas solicitadas por la cátedra (máquinas de estado para extravío/reposición, suspensión por caducidad temporal, colas de reserva FIFO con pre-asignación de 24h, y renovaciones limitadas condicionadas).
4. **Fase de Verificación y Testing:** Desarrollo paralelo de una suite exhaustiva de tests unitarios con aislamiento total (Moq) y pruebas de integración sobre SQLite en memoria (`CustomWebApplicationFactory`), respaldado por una suite de integración manual vía Bruno HTTP.

El equipo demuestra una comprensión profunda de los principios de diseño RESTful, inyección de dependencias y desacoplamiento de capas.

---

## 2. Fortalezas Identificadas

### 2.1. Análisis y Diseño Funcional
* **Trazabilidad Bidireccional Rigurosa:** El documento [`Documentos/Trazabilidad.md`](Documentos/Trazabilidad.md) vincula cada regla de negocio y caso de uso con su endpoint, clase de validación, tests unitarios, tests de integración y archivo `.bru` de Bruno.
* **Especificación Exhaustiva de Casos de Uso:** Cada caso de uso cuenta con una especificación formal que incluye caminos felices, flujos de excepción mapeados a códigos HTTP canónicos (200, 201, 400, 401, 403, 404, 409) y pre/postcondiciones claras.
* **Manejo de Complejidad Avanzada sin Tareas en Segundo Plano:** El diseño de la expiración de la pre-asignación de reservas (24 horas) y el cálculo de préstamos vencidos (mora) se resuelve de manera ingeniosa en tiempo de consulta/transacción (`ProcesarAsignacionesVencidasAsync`), eliminando la necesidad de infraestructura compleja adicional (ej. background workers o schedulers) en el alcance de la entrega.

### 2.2. Arquitectura y Código Fuente
* **Respeto Estricto de la Arquitectura en Capas (N-Tier):**
  * Los **Controllers** están completamente desprovistos de lógica de negocio o queries directas; delegan en los servicios e implementan manejo granular de excepciones HTTP (`try/catch`).
  * Los **Services** encapsulan la totalidad de la lógica de dominio y validaciones, retornando DTOs limpios y desacoplados (`MapToResponseDTO`).
  * Los **Repositories** concentran el acceso exclusivo a datos y aplican `AsNoTracking()` sistemáticamente en todas las operaciones de consulta, optimizando el consumo de memoria y tracking de EF Core.
* **Seguridad y Robustez en Autenticación:**
  * Implementación de hash de contraseñas seguro mediante `Rfc2898DeriveBytes.Pbkdf2` con salt criptográfico de 128 bits, 100.000 iteraciones SHA256 y comparación en tiempo constante (`CryptographicOperations.FixedTimeEquals`).
  * Protección de rutas administrativas con `[Authorize(Roles = "Administrador")]` y validaciones defensivas críticas (imposibilidad de auto-baja, auto-desactivación o revocación de rol que deje al sistema sin administradores activos).
* **Consistencia en Respuestas de Error:** Configuración centralizada de `InvalidModelStateResponseFactory` en `Program.cs` y `ExceptionMiddleware` para capturar cualquier falla imprevista retornando siempre JSON uniforme `{ "message": "..." }` en español.

---

## 3. Análisis de Brechas (Gap Analysis)

A pesar de la alta calidad general, la auditoría identificó ciertas discrepancias conceptuales y desalineaciones entre la documentación, el diseño y el código implementado:

| Área Auditada | Documentación / Casos de Uso | Implementación en Código Fuente | Brecha Detectada |
| :--- | :--- | :--- | :--- |
| **Actor Lector en la API** | CU-03 indica como actor secundario al *Lector/Alumno*, y el disparador contempla consultas de lectores. | Todos los endpoints de libros (`/api/libros`) tienen `[Authorize]`, exigiendo JWT de usuario interno (`Administrador` o `Bibliotecario`). | El lector no tiene acceso real a la API. Las consultas las realiza el bibliotecario en su nombre. La documentación debe explicitar que el actor del sistema es únicamente el operador interno. |
| **Baja de Libros y Lectores vs. Restricciones** | RN-09 y CU-02/CU-04 documentaban originalmente no eliminar si hay préstamos activos. | `LibroService.DeleteAsync` y `LectorService.DeleteAsync` rechazan la baja si hay préstamos activos, reservas activas **o historial pasado de préstamos** (`TienePrestamosPorLibroAsync`). Además, `Reserva` tiene cascade delete en base de datos. | **Endurecimiento de regla de negocio**: El código implementa un borrado preventivo total (preservación de auditoría histórica). Existe discrepancia con `OnDelete(DeleteBehavior.Cascade)` en la relación `Reserva` en `DbContext`. |
| **Validación de Concurrencia en Stock y Reservas** | Se describe asignación atómica de stock y reservas en 24h. | Se utilizan transacciones implícitas de EF Core en lecturas separadas de escrituras sin bloqueos optimistas (`[Timestamp]`/`RowVersion`) ni transacciones de base de datos explícitas (`IDbContextTransaction`). | Riesgo de **Race Conditions** bajo alta concurrencia: dos préstamos simultáneos sobre el último ejemplar podrían otorgarse ambos. |
| **Filtros en Consultas Masivas** | CU-02 y CU-04 proponen listados generales de catálogo y padrón. | `GetAllAsync` en Libros, Lectores y Préstamos no implementa paginación (`skip`/`take`). | **NFR de Escalabilidad/Rendimiento**: Con un crecimiento realista a miles de ejemplares o transacciones, traer colecciones completas impactará el rendimiento de la red y memoria. |

---

## 4. Conclusión de Arquitectura y Calidad

El código demuestra un apego notable a las buenas prácticas de **Clean Code**, principios **SOLID** y diseño de APIs RESTful. Las responsabilidades están claramente delimitadas, los nombres de clases y métodos son altamente expresivos, y el acoplamiento es bajo gracias a la inversión de dependencias.

Los aspectos a mejorar detectados no invalidan la excelente base técnica, sino que corresponden al paso natural hacia un producto apto para entornos productivos reales:
1. **Transaccionalidad y Concurrencia:** Incorporar control de concurrencia optimista en entidades con inventario limitado (`Libro.Stock`) y formalizar `BeginTransactionAsync` al cerrar reservas y préstamos en simultáneo.
2. **Gestión de Secretos:** Extraer la clave simétrica JWT y la conexión a base de datos fuera de los archivos `appsettings.json` versionados, empleando User Secrets o variables de entorno.
3. **Paginación:** Implementar paginación estándar en los endpoints de consulta masiva (`GET /api/libros`, `GET /api/lectores`).

**Calificación de la Auditoría:** **Sobresaliente (9.5/10)**. El proyecto cumple con la totalidad de los requisitos pedagógicos y funcionales, estableciendo un estándar de referencia en la cátedra.

