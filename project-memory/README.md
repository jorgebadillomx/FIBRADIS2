# project-memory — Memoria Persistente de FIBRADIS

Sistema de memoria compartida entre agentes de IA (Claude, Codex, etc.) usando **mem0**.

## Estructura

```
project-memory/
  README.md          ← este archivo
  mem0.db            ← base de datos SQLite de mem0 (generada por setup_mem0.py)
  seeds/             ← archivos semilla con el conocimiento inicial del proyecto
    00-project-overview.md
    01-architecture.md
    02-modules.md
    03-conventions.md
    04-api-patterns.md
    05-current-status.md
    06-agent-workflows.md
```

## Cómo usar mem0 en este proyecto

### Opción A — mem0 managed platform (recomendada, sin API de OpenAI/Anthropic)

1. Regístrate gratis en https://app.mem0.ai y copia tu API key
2. Configura la key y arranca:

```powershell
$env:MEM0_MODE    = "managed"
$env:MEM0_API_KEY = "m0-..."
pip install -r scripts/memory/requirements.txt
python scripts/memory/setup_mem0.py
```

### Opción B — Ollama local (completamente offline, sin ninguna API key)

1. Instala Ollama desde https://ollama.com
2. Descarga los modelos y arranca:

```powershell
ollama pull nomic-embed-text
ollama pull llama3.2
$env:MEM0_MODE = "ollama"
pip install -r scripts/memory/requirements.txt
python scripts/memory/setup_mem0.py
```

### Opción C — Anthropic API + embeddings locales

Solo si tienes una Anthropic API key (distinta de la suscripción claude.ai):

```powershell
$env:MEM0_MODE        = "anthropic"
$env:ANTHROPIC_API_KEY = "sk-ant-..."
pip install -r scripts/memory/requirements.txt
python scripts/memory/setup_mem0.py
```

Esto carga todos los archivos `seeds/*.md` en mem0.

### Buscar contexto antes de trabajar

```bash
python scripts/memory/memory_cli.py search "portafolio módulo arquitectura"
python scripts/memory/memory_cli.py search "auth JWT roles"
python scripts/memory/memory_cli.py list
```

### Agregar nueva memoria

```bash
python scripts/memory/memory_cli.py add "Decisión: se usa Qdrant local para X porque Y"
python scripts/memory/memory_cli.py add "Bug resuelto: el score de oportunidades falla cuando LTV es null — redistribuir peso"
```

## Por qué mem0

mem0 permite que distintos agentes (Claude Code, Codex, futuros agentes) compartan contexto del proyecto de forma semántica. En lugar de releer todo el PRD en cada sesión, los agentes pueden buscar sólo lo relevante para la tarea actual.

## Agente responsable de actualizar

Cualquier agente que tome una **decisión de diseño**, resuelva un **bug no obvio**, o detecte un **patrón importante** debe agregar una memoria. Las decisiones efímeras (qué archivo edité hoy) NO van aquí.
