"""
memory_cli.py - CLI para la memoria persistente de FIBRADIS en mem0

Comandos:
    python scripts/memory/memory_cli.py search "<query>"
    python scripts/memory/memory_cli.py add "<texto>"
    python scripts/memory/memory_cli.py list
    python scripts/memory/memory_cli.py delete <memory_id>

Configuracion (mismas variables que setup_mem0.py):
    $env:MEM0_MODE    = "managed" | "ollama" | "anthropic"  (default: managed)
    $env:MEM0_API_KEY = "m0-..."   (solo modo managed)
"""

import os
import sys
from pathlib import Path

# Forzar UTF-8 en Windows para manejar caracteres especiales de mem0
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

REPO_ROOT = Path(__file__).parent.parent.parent
DB_PATH = str(REPO_ROOT / "project-memory" / "mem0.db")
AGENT_USER_ID = "fibradis-project"


def _load_dotenv():
    env_file = REPO_ROOT / ".env"
    if not env_file.exists():
        return
    for line in env_file.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line and not line.startswith("#") and "=" in line:
            key, _, value = line.partition("=")
            os.environ.setdefault(key.strip(), value.strip())


_load_dotenv()


def get_client():
    try:
        from mem0 import Memory, MemoryClient
    except ImportError:
        print("ERROR: mem0ai no instalado. Ejecuta: pip install -r scripts/memory/requirements.txt")
        sys.exit(1)

    mode = os.environ.get("MEM0_MODE", "managed").lower()

    if mode == "managed":
        api_key = os.environ.get("MEM0_API_KEY", "")
        if not api_key:
            print("ERROR: Configura MEM0_API_KEY (obtén tu key gratis en https://app.mem0.ai)")
            sys.exit(1)
        return MemoryClient(api_key=api_key), True

    elif mode == "ollama":
        config = {
            "vector_store": {
                "provider": "chroma",
                "config": {
                    "collection_name": "fibradis",
                    "path": str(REPO_ROOT / "project-memory" / "chroma_db"),
                },
            },
            "llm": {"provider": "ollama", "config": {"model": "llama3.2", "ollama_base_url": "http://localhost:11434"}},
            "embedder": {"provider": "ollama", "config": {"model": "nomic-embed-text", "ollama_base_url": "http://localhost:11434"}},
            "history_db_path": DB_PATH,
        }
        return Memory.from_config(config), False

    elif mode == "anthropic":
        api_key = os.environ.get("ANTHROPIC_API_KEY", "")
        if not api_key:
            print("ERROR: Configura ANTHROPIC_API_KEY")
            sys.exit(1)
        config = {
            "vector_store": {
                "provider": "chroma",
                "config": {"collection_name": "fibradis", "path": str(REPO_ROOT / "project-memory" / "chroma_db")},
            },
            "llm": {"provider": "anthropic", "config": {"model": "claude-3-5-haiku-20241022", "api_key": api_key}},
            "embedder": {"provider": "huggingface", "config": {"model": "multi-qa-MiniLM-L6-cos-v1"}},
            "history_db_path": DB_PATH,
        }
        return Memory.from_config(config), False

    print(f"ERROR: Modo '{mode}' no reconocido.")
    sys.exit(1)


def cmd_search(client, is_managed, query):
    if is_managed:
        results = client.search(query, filters={"user_id": AGENT_USER_ID}, limit=5)
    else:
        results = client.search(query, user_id=AGENT_USER_ID, limit=5)

    memories = results if isinstance(results, list) else results.get("results", [])
    if not memories:
        print("Sin resultados.")
        return

    print(f"\n=== {len(memories)} resultado(s) para: '{query}' ===\n")
    for i, r in enumerate(memories, 1):
        mem_id = r.get("id", "---")
        score = r.get("score", 0)
        text = r.get("memory", "")
        print(f"[{i}] {mem_id} | score={score:.3f}")
        print(f"    {text[:350]}{'...' if len(text) > 350 else ''}\n")


def cmd_add(client, is_managed, text):
    result = client.add(text, user_id=AGENT_USER_ID, metadata={"source": "agent"})
    print("OK: Memoria agregada.")
    if isinstance(result, dict) and result.get("results"):
        print(f"  ID: {result['results'][0].get('id', '---')}")


def cmd_list(client, is_managed):
    if is_managed:
        results = client.get_all(filters={"user_id": AGENT_USER_ID})
    else:
        results = client.get_all(user_id=AGENT_USER_ID)

    memories = results if isinstance(results, list) else results.get("results", [])
    if not memories:
        print("No hay memorias almacenadas.")
        return

    print(f"\n=== {len(memories)} memoria(s) ===\n")
    for i, r in enumerate(memories[:30], 1):
        mem_id = r.get("id", "---")
        text = r.get("memory", "")
        source = r.get("metadata", {}).get("source", "")
        print(f"[{i}] {mem_id} [{source}]")
        print(f"    {text[:200]}{'...' if len(text) > 200 else ''}\n")


def cmd_delete(client, is_managed, memory_id):
    client.delete(memory_id)
    print(f"OK: Memoria {memory_id} eliminada.")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    command = sys.argv[1].lower()
    client, is_managed = get_client()

    if command == "search":
        if len(sys.argv) < 3:
            print("Uso: memory_cli.py search '<query>'")
            sys.exit(1)
        cmd_search(client, is_managed, " ".join(sys.argv[2:]))

    elif command == "add":
        if len(sys.argv) < 3:
            print("Uso: memory_cli.py add '<texto>'")
            sys.exit(1)
        cmd_add(client, is_managed, " ".join(sys.argv[2:]))

    elif command == "list":
        cmd_list(client, is_managed)

    elif command == "delete":
        if len(sys.argv) < 3:
            print("Uso: memory_cli.py delete <memory_id>")
            sys.exit(1)
        cmd_delete(client, is_managed, sys.argv[2])

    else:
        print(f"Comando desconocido: {command}")
        print(__doc__)
        sys.exit(1)
