---
name: bmad-testing-guide
description: 'Genera una guía de pruebas formal y en lenguaje humano para testers que no conocen el sistema, basada en épicas completadas. Use when the user says "genera guía de pruebas", "crea testing guide", "arma guía de pruebas para la épica X" o "testing guide"'
---

# Testing Guide Workflow

**Goal:** Generar una guía de pruebas formal, en lenguaje humano y accesible, para un tester que no participó en el desarrollo del sistema. La guía cubre una o varias épicas completadas y sirve como documento de aceptación formal.

**Your Role:** Eres un QA Lead con experiencia en redacción técnica. Tu trabajo es traducir lo que se construyó en instrucciones claras que cualquier tester pueda seguir sin conocer el código ni el historial del proyecto.
- Comunica todas las respuestas en {communication_language}, adaptadas a {user_skill_level}
- Genera el documento final en {document_output_language}
- Usa lenguaje humano, directo y sin jerga innecesaria en la guía — si un término técnico es inevitable, defínelo en el glosario
- El documento debe ser utilizable como formalidad de pruebas: puede entregarse a un tercero, a un cliente, o usarse en una auditoría
- Nunca asumas que el tester sabe cómo funciona el sistema por dentro; escribe como si fuera su primer día

## Conventions

- Bare paths resolve from the skill root.
- `{skill-root}` resolves to this skill's installed directory (where `customize.toml` lives).
- `{project-root}`-prefixed paths resolve from the project working directory.
- `{skill-name}` resolves to the skill directory's basename.

## On Activation

### Step 1: Resolve the Workflow Block

Run: `python3 {project-root}/_bmad/scripts/resolve_customization.py --skill {skill-root} --key workflow`

**If the script fails**, resolve the `workflow` block yourself reading these three files in base → team → user order:

1. `{skill-root}/customize.toml` — defaults
2. `{project-root}/_bmad/custom/{skill-name}.toml` — team overrides
3. `{project-root}/_bmad/custom/{skill-name}.user.toml` — personal overrides

Any missing file is skipped. Scalars override, tables deep-merge, arrays append.

### Step 2: Execute Prepend Steps

Execute each entry in `{workflow.activation_steps_prepend}` in order before proceeding.

### Step 3: Load Persistent Facts

Treat every entry in `{workflow.persistent_facts}` as contexto que mantienes durante todo el run. Entries prefixed `file:` are paths or globs under `{project-root}` — load the referenced contents as facts.

### Step 4: Load Config

Load config from `{project-root}/_bmad/bmm/config.yaml` and resolve:

- `project_name`, `user_name`
- `communication_language`, `document_output_language`
- `user_skill_level`
- `planning_artifacts`, `implementation_artifacts`
- `date` as system-generated current datetime

### Step 5: Greet the User

Greet `{user_name}`, speaking in `{communication_language}`.

### Step 6: Execute Append Steps

Execute each entry in `{workflow.activation_steps_append}` in order.

Activation complete. Begin the workflow below.

## Paths

- `sprint_status_file` = `{implementation_artifacts}/sprint-status.yaml`
- `epics_file` = `{planning_artifacts}/epics.md`

## Input Files

| Input | Description | Path Pattern(s) | Load Strategy |
|-------|-------------|------------------|---------------|
| sprint_status | Estado actual de todas las historias y épicas | `{implementation_artifacts}/sprint-status.yaml` | FULL_LOAD |
| epics | Definición de épicas, RFs cubiertos, criterios | whole: `{planning_artifacts}/*epic*.md` | SELECTIVE_LOAD |
| stories | Story files de la épica seleccionada | `{implementation_artifacts}/{{epic_number}}-*.md` | FULL_LOAD |
| prd | Requerimientos del producto para contexto | whole: `{planning_artifacts}/*prd*.md` | FULL_LOAD |
| architecture | Arquitectura del sistema para contexto y glosario | whole: `{planning_artifacts}/*architecture*.md` | SELECTIVE_LOAD |

## Execution

<workflow>

<critical>Comunica todas las respuestas en {communication_language}, adaptadas a {user_skill_level}</critical>
<critical>Genera el documento final en {document_output_language}</critical>
<critical>La guía debe ser legible por un tester sin conocimiento previo del sistema</critical>
<critical>Ejecuta TODOS los pasos en orden; NO saltes pasos</critical>

<!-- ═══════════════════════════════════════════════════════════ -->
<step n="1" goal="Descubrir épicas completadas y elegir cuál documentar">

<action>Carga el archivo {sprint_status_file}</action>

<action>Lee todas las entradas de development_status</action>

<action>Identifica todas las épicas que tienen AL MENOS UNA historia con status "done"</action>

<action>Para cada épica encontrada, recopila:
- Número de épica (epic-X)
- Cantidad de historias totales
- Cantidad de historias en status "done"
- Si el status de la épica es "done", "in-progress" o "backlog"
- Si tiene retrospectiva completada ("epic-X-retrospective": "done")
</action>

<action>Filtra: muestra solo las épicas donde al menos 1 historia esté "done"</action>

<check if="ninguna épica tiene historias done">
  <output>
No encontré épicas con historias completadas en {sprint_status_file}.

Para generar una guía de pruebas necesito al menos una historia marcada como "done". Cuando completes historias de desarrollo, vuelve a ejecutar este skill.
  </output>
  <action>HALT</action>
</check>

<check if="hay épicas con historias done">
  <output>
Encontré las siguientes épicas con trabajo completado:

{{#each completed_epics}}
**Épica {{epic_number}}** — {{epic_title}}
- Historias completadas: {{done_count}}/{{total_count}}
- Estado general: {{epic_status}}
{{#if retro_done}}- Retrospectiva: completada ✅{{/if}}
{{/each}}

¿Para cuál de estas épicas quieres generar la guía de pruebas, {user_name}?

Puedes indicar el número (ej. "1", "2") o decirme si quieres cubrir varias.
  </output>

  <action>WAIT for {user_name} to select epic(s)</action>

  <action>Set {{selected_epics}} = epic numbers chosen by user</action>

  <check if="user selects multiple epics">
    <output>
Perfecto, generaré una guía de pruebas unificada que cubra las épicas {{selected_epics}}.
    </output>
  </check>

  <check if="user selects single epic">
    <output>
Perfecto, generaré la guía de pruebas para la Épica {{selected_epic}}.
    </output>
  </check>
</check>

</step>

<!-- ═══════════════════════════════════════════════════════════ -->
<step n="2" goal="Cargar todos los documentos necesarios">

<action>Para cada épica en {{selected_epics}}:</action>

<action>1. Carga la sección correspondiente de {epics_file} — extrae:
- Descripción de la épica
- RFs cubiertos
- NFRs aplicables
- Historia por historia: nombre, criterios de aceptación
</action>

<action>2. Carga todos los story files en {implementation_artifacts} que coincidan con el patrón {{epic_number}}-*.md:
- Para cada story file extrae:
  - Nombre y descripción de la historia
  - Criterios de Aceptación (acceptance criteria) completos
  - Notas de implementación relevantes (Dev Notes, Implementation Notes)
  - Lista de archivos modificados (File List) si existe
  - Cualquier limitación o caso conocido documentado
</action>

<action>3. Del PRD, extrae el contexto de usuario relevante para esta épica:
- User journeys que corresponden a estas funcionalidades
- Usuarios objetivo (tester necesita saber QUIÉN usa qué)
- Restricciones de negocio relevantes
</action>

<action>4. De la arquitectura, extrae solo lo necesario para el tester:
- Superficies del sistema (pública, privada, ops)
- Nombres de módulos relevantes
- Comportamiento esperado de errores y estados de datos
</action>

<output>
Cargué todos los documentos. Ahora voy a analizar el contenido para construir la guía.

Épica(s) seleccionada(s): {{selected_epics}}
Historias encontradas: {{total_stories_found}}
RFs cubiertos: {{rf_count}}
</output>

</step>

<!-- ═══════════════════════════════════════════════════════════ -->
<step n="3" goal="Analizar y organizar el contenido de la guía">

<action>Construye el mapa de funcionalidades a probar:</action>

**Por cada historia en la épica seleccionada:**

1. **Funcionalidad entregada** — ¿qué puede hacer el usuario ahora que antes no podía?
2. **Criterios de aceptación** — conviértelos en casos de prueba en lenguaje humano
3. **Casos borde** — identifica condiciones especiales mencionadas (datos vacíos, errores, límites)
4. **Validaciones negativas** — ¿qué debería rechazar o impedir el sistema?
5. **Prerrequisitos** — ¿qué necesita estar configurado o existir para probar esto?

**Identifica lo que NO abarca la guía:**
- RFs marcados como GROWTH (excluidos del MVP)
- Funcionalidades de épicas no completadas
- Casos que requieren integración con sistemas externos aún no disponibles
- Funcionalidades administrativas que requieren acceso especial no incluido en la épica

**Construye el glosario:**
- Extrae todos los términos del sistema que un tester externo no conocería
- Incluye: nombres de módulos, términos de negocio (FIBRA, ticker, yield, etc.), estados del sistema, roles de usuario

**Identifica el ambiente requerido:**
- Qué datos semilla son necesarios
- Qué roles de usuario deben existir
- Qué configuración debe estar activa

<output>
Análisis completo. Identificadas {{test_case_count}} situaciones de prueba organizadas en {{section_count}} secciones funcionales.
</output>

</step>

<!-- ═══════════════════════════════════════════════════════════ -->
<step n="4" goal="Redactar y guardar la guía de pruebas">

<action>Genera el documento completo siguiendo exactamente la plantilla a continuación</action>

<action>Usa lenguaje directo, sin tecnicismos innecesarios</action>

<action>Cada escenario de prueba debe ser ejecutable por alguien que nunca usó el sistema</action>

<action>Para el nombre del archivo: testing-guide-epic-{{epic_numbers_joined}}-{date}.md</action>
<action>Guarda en: {implementation_artifacts}/testing-guide-epic-{{epic_numbers_joined}}-{date}.md</action>

---

### PLANTILLA DEL DOCUMENTO

```markdown
---
project: {project_name}
epics: [{{selected_epics}}]
version: 1.0
date: {date}
status: draft
prepared_by: QA Lead (bmad-testing-guide)
---

# Guía de Pruebas — {{epic_titles_joined}}
## {project_name}

---

## 1. Propósito de este documento

Este documento es una guía de pruebas funcionales para el sistema **{project_name}**. Está escrito para testers que no participaron en el desarrollo y que pueden no conocer el sistema previamente.

El objetivo es verificar que las funcionalidades entregadas en {{epic_scope_description}} funcionan correctamente desde la perspectiva del usuario. No se requiere conocimiento técnico del código; solo necesitas acceso al sistema y seguir los pasos descritos aquí.

Si algo no está claro o encuentras un comportamiento no documentado, regístralo como hallazgo aunque no estés seguro de si es un error.

---

## 2. Alcance de esta guía

Esta guía cubre las funcionalidades entregadas en las siguientes épicas:

{{#each selected_epics}}
### Épica {{epic_number}}: {{epic_title}}
{{epic_description}}

**Qué abarca:**
{{#each covered_rfs}}
- {{rf_description}}
{{/each}}
{{/each}}

---

## 3. Fuera de alcance

Las siguientes áreas **no** son parte de esta guía de pruebas y no deben evaluarse en esta ronda:

{{#each out_of_scope_items}}
- **{{item_name}}**: {{reason_excluded}}
{{/each}}

> **Nota importante:** Si durante las pruebas descubres funcionalidades que no están en este documento, no asumas que están bien o mal — regístralas como hallazgo para que el equipo las evalúe.

---

## 4. Antes de empezar

### 4.1 Prerrequisitos

Para ejecutar estas pruebas necesitas lo siguiente:

**Acceso al sistema:**
{{#each access_requirements}}
- {{access_item}}
{{/each}}

**Datos que deben existir:**
{{#each data_requirements}}
- {{data_item}}
{{/each}}

**Herramientas:**
- Navegador web actualizado (Chrome, Firefox o Edge recomendados)
- Conexión a internet
{{#each additional_tools}}
- {{tool}}
{{/each}}

### 4.2 Roles de usuario disponibles

El sistema tiene distintos tipos de usuario con diferentes permisos:

{{#each user_roles}}
**{{role_name}}**
- ¿Qué puede hacer?: {{role_description}}
- Cómo acceder: {{role_access}}

{{/each}}

### 4.3 Glosario

Si encuentras alguno de estos términos en la guía y no sabes qué significa, consulta aquí:

| Término | Significado |
|---------|-------------|
{{#each glossary_terms}}
| **{{term}}** | {{definition}} |
{{/each}}

---

## 5. Escenarios de prueba

{{#each test_sections}}

---

### 5.{{section_number}}. {{section_title}}

**¿Qué es esto?**
{{section_human_description}}

**¿Quién lo usa?**
{{section_user_context}}

---

{{#each test_cases}}

#### Prueba {{parent_section}}.{{case_number}}: {{case_title}}

**Objetivo:** {{case_objective}}

**Prerequisitos específicos:**
{{#each prerequisites}}
- {{prerequisite}}
{{/each}}

**Pasos:**

{{#each steps}}
{{step_number}}. {{step_description}}
   {{#if expected_result}}→ *Debes ver:* {{expected_result}}{{/if}}
{{/each}}

**¿Qué debes ver al terminar?**
{{expected_final_state}}

**Criterio de éxito:** {{success_criterion}}

{{#if edge_cases}}
**Casos especiales a verificar:**
{{#each edge_cases}}
- {{edge_case}}
{{/each}}
{{/if}}

---

{{/each}}
{{/each}}

---

## 6. Pruebas negativas

Estas pruebas verifican que el sistema rechaza correctamente acciones inválidas o incorrectas. En todos los casos el sistema **no debe fallar silenciosamente** — debe mostrar un mensaje claro al usuario.

{{#each negative_tests}}

#### Prueba N{{test_number}}: {{test_title}}

**Qué intenta hacer el tester:** {{tester_action}}

**Qué debe hacer el sistema:** {{expected_rejection_behavior}}

**Criterio de éxito:** {{success_criterion}}

---

{{/each}}

---

## 7. Checklist de aceptación

Antes de firmar que la prueba fue exitosa, verifica que todos los puntos siguientes están cubiertos:

### Funcionalidad general
{{#each general_checklist}}
- [ ] {{checklist_item}}
{{/each}}

### Calidad visual y usabilidad
- [ ] Las páginas se muestran correctamente en pantalla de escritorio (1280px o más)
- [ ] Las páginas se muestran correctamente en tableta (768px)
- [ ] No hay texto cortado, botones fuera de lugar ni elementos superpuestos
- [ ] Los mensajes de error son comprensibles para un usuario normal
- [ ] Los estados de carga (cuando el sistema está procesando) son visibles

### Manejo de errores
- [ ] Cuando algo falla, el sistema muestra un mensaje claro — no una pantalla en blanco ni un error técnico
- [ ] Después de un error, el usuario puede intentarlo de nuevo sin recargar la página
{{#each error_handling_checks}}
- [ ] {{check_item}}
{{/each}}

### Requerimientos específicos de esta épica
{{#each epic_specific_checks}}
- [ ] {{check_item}} *({{rf_trace}})*
{{/each}}

---

## 8. Registro de resultados

Usa esta sección para documentar los resultados de tus pruebas.

| # Prueba | Resultado | Observaciones | Fecha |
|----------|-----------|---------------|-------|
{{#each all_test_cases}}
| {{test_id}} | ⬜ Pendiente / ✅ Pasa / ❌ Falla | | |
{{/each}}

**Hallazgos encontrados:**

| # | Descripción | Sección | Severidad | Estado |
|---|-------------|---------|-----------|--------|
| 1 | | | | |

**Severidad:**
- **Crítica** — El sistema no funciona o pierde datos
- **Alta** — Una funcionalidad principal no opera como se espera
- **Media** — Comportamiento incorrecto pero existe alternativa
- **Baja** — Problema estético o de redacción

---

## 9. Notas finales

{{final_notes}}

---

*Documento generado por bmad-testing-guide el {date}.*
*Para preguntas sobre el alcance o los criterios, contactar al equipo de desarrollo.*
```

---

<action>Después de generar el documento, muestra al usuario un resumen de lo que se incluyó</action>

<output>

---

✅ **Guía de pruebas generada**

**Archivo:** {implementation_artifacts}/testing-guide-epic-{{epic_numbers_joined}}-{date}.md

**Resumen del documento:**
- Épicas cubiertas: {{selected_epics}}
- Secciones de prueba: {{section_count}}
- Casos de prueba funcionales: {{functional_test_count}}
- Pruebas negativas: {{negative_test_count}}
- Items en checklist de aceptación: {{checklist_count}}

**Fuera de alcance documentado:** {{out_of_scope_count}} áreas excluidas con justificación.

¿Quieres ajustar algo en la guía? Puedo:
- Agregar más detalle en alguna sección específica
- Reformular casos de prueba para que sean más claros
- Agregar escenarios de prueba que no estén cubiertos
- Ajustar el nivel de detalle al público objetivo del tester

</output>

</step>

</workflow>

<writing-guidelines>
<guideline>Escribe cada paso de prueba como si le explicaras a alguien que nunca usó el sistema — sin asumir contexto previo</guideline>
<guideline>Los "Debes ver" deben describir el resultado exacto observable, no la lógica interna</guideline>
<guideline>Usa verbos imperativos simples en los pasos: "Haz clic en...", "Escribe...", "Navega a...", "Verifica que..."</guideline>
<guideline>El glosario debe incluir TODOS los términos del dominio que un externo no reconocería</guideline>
<guideline>Los criterios de éxito deben ser binarios: pasa o falla, sin ambigüedad</guideline>
<guideline>Los casos borde son tan importantes como el happy path — no los omitas</guideline>
<guideline>El "Fuera de alcance" debe explicar POR QUÉ algo no está incluido, no solo qué</guideline>
<guideline>El documento debe poder entregarse a un cliente o auditor sin necesidad de explicación adicional</guideline>
</writing-guidelines>
