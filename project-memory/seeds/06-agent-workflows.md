# FIBRADIS — Workflows de Agentes e Historias

## Workflow obligatorio para historias creadas con BMAD

Instrucción explícita del usuario:

- Al crear cualquier historia nueva con `/bmad-create-story`, se debe crear inmediatamente un branch dedicado con el mismo nombre que la historia.
- Formato esperado del branch: `story/<slug-de-la-historia>`.
- Ejemplo: `story/2-3-ficha-publica-de-fibra`.
- Ninguna historia debe desarrollarse en `main`.
- Ninguna historia debe desarrollarse en el branch de otra historia.
- Cuando la historia termina y pasa a `done`, se hace commit en ese branch y luego se mergea a `main`.

## Implicaciones operativas para agentes

- Si un agente crea una historia nueva, debe crear el branch dedicado antes de implementar cualquier cambio de esa historia.
- Si el agente detecta que está en `main` o en un branch ajeno a la historia activa, debe detener el desarrollo de esa historia y corregir el workflow.
- Este workflow aplica tanto a Claude como a Codex y debe tratarse como memoria persistente del proyecto.
