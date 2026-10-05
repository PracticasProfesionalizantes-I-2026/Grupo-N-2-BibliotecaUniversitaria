# Grupo-N-2-BibliotecaUniversitaria

Integrantes: Lautaro Navarro y Elías Korell



Documentación

BiblioGest es un sistema de gestión de biblioteca universitaria. Esta API
cubre exclusivamente los 3 casos de uso principales del sistema —
**Gestionar Libros**, **Gestionar Lectores** y **Gestionar Préstamos** —
según lo acordado con la cátedra.



https://docs.google.com/document/d/1l-hgM-n6PiBafRuCns1cQy5qqAaQTxlvS21iAa8c9ww/edit?tab=t.0



PRESENTACIÓN de app BiblioGest:



https://1drv.ms/p/c/f7646867687d1003/IQCJK54DJMozRJ3uwkxZlD3hAWH1imrQJQQrl5GPhNG3fgY?e=jVX4eW



PRESENTACION DE CASOS DE USO:

PRESENTACION DE CASOS DE USO:

| Regla | Excepción (`Shared/Exceptions/`) | HTTP |
| --- | --- | --- |
| Datos obligatorios del libro faltantes | `LibroInvalidoException` | 400 |
| Stock negativo | `LibroInvalidoException` | 400 |
| Datos obligatorios del lector faltantes | `LectorInvalidoException` | 400 |
| Libro/Lector/Préstamo inexistente | `LibroNotFoundException` / `LectorNotFoundException` / `PrestamoNotFoundException` | 404 |
| Eliminar libro con préstamos activos | `LibroConPrestamosActivosException` | 409 |
| Eliminar lector con préstamos activos | `LectorConPrestamosActivosException` | 409 |
| Identificador (DNI/legajo) duplicado | `IdentificadorDuplicadoException` | 409 |

https://docs.google.com/document/d/1cPGvxmILelmMMogMiCeEFB1LkpB8VdPgWXqcZl9IrxY/edit?usp=sharing


https://docs.google.com/document/d/1cPGvxmILelmMMogMiCeEFB1LkpB8VdPgWXqcZl9IrxY/edit?usp=sharing



MOCAP:



https://www.figma.com/design/wegjr31mBIEI3hgwNyycAH/Mocap---BiblioGest?node-id=0-1\&m=dev\&t=xQyKmKHf32WRdCLE-1

### Libros

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/libros?busqueda=` | Listar/buscar por título, autor o ISBN | 200 | — |
| GET | `/api/libros/{id}` | Obtener un libro | 200 | 404 |
| POST | `/api/libros` | Crear libro | 201 | 400 |
| PUT | `/api/libros/{id}` | Modificar libro | 200 | 400, 404 |
| DELETE | `/api/libros/{id}` | Eliminar libro | 200 | 404, 409 |

Ejemplo `POST /api/libros`:

```json
// Request
{
  "titulo": "El Aleph",
  "autor": "Jorge Luis Borges",
  "isbn": "978-8420633109",
  "ubicacion": "Estante A4",
  "stock": 4
}

// Response 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "titulo": "El Aleph",
  "autor": "Jorge Luis Borges",
  "isbn": "978-8420633109",
  "ubicacion": "Estante A4",
  "stock": 4,
  "disponible": true
}
```

### Lectores

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/lectores` | Listar lectores | 200 | — |
| GET | `/api/lectores/{id}` | Obtener un lector | 200 | 404 |
| POST | `/api/lectores` | Crear lector | 201 | 400, 409 |
| PUT | `/api/lectores/{id}` | Modificar lector | 200 | 400, 404, 409 |
| DELETE | `/api/lectores/{id}` | Eliminar lector | 200 | 404, 409 |

Ejemplo `POST /api/lectores`:

```json
// Request
{
  "nombre": "Facundo",
  "apellido": "López",
  "email": "facundo.lopez@example.com",
  "identificador": "35555111"
}

// Response 201 Created
{
  "id": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "nombre": "Facundo",
  "apellido": "López",
  "email": "facundo.lopez@example.com",
  "identificador": "35555111"
}
```

### Préstamos

| Método | Ruta | Descripción | Éxito | Errores |
| --- | --- | --- | --- | --- |
| GET | `/api/prestamos` | Listar préstamos | 200 | — |
| GET | `/api/prestamos/{id}` | Obtener un préstamo | 200 | 404 |
| POST | `/api/prestamos` | Crear préstamo | 201 | 404 |
| PUT | `/api/prestamos/{id}/devolucion` | Registrar devolución | 200 | 404 |

Ejemplo `POST /api/prestamos`:

```json
// Request
{
  "lectorId": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "libroId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}

// Response 201 Created
{
  "id": "c9d8e7f6-a5b4-3210-9876-543210fedcba",
  "lectorId": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",
  "lectorNombreCompleto": "Facundo López",
  "libroId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "libroTitulo": "El Aleph",
  "fechaPrestamo": "2026-09-18T01:00:00Z",
  "fechaVencimiento": "2026-10-02T01:00:00Z",
  "fechaDevolucion": null,
  "estado": "Activo"
}
```

## Colección de Bruno

En `bruno/` hay una carpeta por controller con un request por caso de éxito
y de error relevante (400/404/409). El entorno `Local` trae `baseUrl` y los
requests de creación completan automáticamente `libroId`/`lectorId`/
`prestamoId` para encadenar los siguientes.

## Testing

- `tests/BiblioGest.UnitTests`: xUnit + Moq sobre los 3 services, sin tocar
  la base real (20 tests).
- `tests/BiblioGest.IntegrationTests`: `WebApplicationFactory` levantando la
  API completa contra una SQLite en memoria aislada por instancia (12 tests).
