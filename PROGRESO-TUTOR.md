# Progreso del proyecto (modo tutor)

Archivo mantenido por el tutor (IA). Es el único archivo del repo que edita.
Al empezar cada sesión se lee para saber en qué quedamos.

- **Equipo:** Elías Korell y Lautaro Navarro — Grupo N°2, Prácticas Profesionalizantes I 2026.
- **Consigna:** `actividades/prompt-arquitectura-capas.md` del `demo-repository` de la cátedra.
- **Flujo de trabajo:** una rama por etapa/caso de uso desde `develop`, PR a `develop` con revisión cruzada (uno escribe, el otro revisa).

## Decisión pendiente (antes de la Etapa 1)

En `develop` existía una implementación completa generada con IA (commit `7920d60`).
2026-10-05: Lautaro avisa que en el remoto hay un commit que revierte todo eso, así que vamos por
reconstruir a mano desde cero (camino A). Próximo paso: pull, confirmar que el revert está y arrancar la Etapa 1.

## Etapas

| # | Etapa | Estado | Escribe | Revisa | Rama |
|---|-------|--------|---------|--------|------|
| 1 | Estructura de la solución (proyectos y referencias) | Pendiente | — | — | `feature/estructura-solucion` |
| 2 | Entidades + DbContext + migración + DbInitializer | Pendiente | — | — | `feature/dataaccess-base` |
| 3 | Shared: DTOs y excepciones base | Pendiente | — | — | `feature/shared-dtos-excepciones` |
| 4 | CU-02 Gestionar Libros (repo → service → controller → tests unitarios → Bruno) | Pendiente | — | — | `feature/cu-02-libros` |
| 5 | CU-04 Gestionar Lectores (vertical completa) | Pendiente | — | — | `feature/cu-04-lectores` |
| 6 | CU-05 Gestionar Préstamos + mora (CU-06) | Pendiente | — | — | `feature/cu-05-prestamos` |
| 7 | Tests de integración (WebApplicationFactory) + Bruno completo | Pendiente | — | — | `feature/tests-integracion` |
| 8 | Cierre: README.md, AGENTS.md y checklist de calidad | Pendiente | — | — | `docs/cierre` |

## Conceptos o errores para repasar

_(vacío por ahora)_
