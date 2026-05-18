"""
setup_mem0.py — Inicializa mem0 y carga los archivos semilla de project-memory/seeds/

MODOS SOPORTADOS (configura con variable de entorno MEM0_MODE):

  1. managed (recomendado) — mem0.ai cloud, tier gratuito, sin API de OpenAI/Anthropic
       $env:MEM0_MODE = "managed"
       $env:MEM0_API_KEY = "m0-..."   ← obtenlo en https://app.mem0.ai

  2. ollama — completamente local, sin ninguna API key
       $env:MEM0_MODE = "ollama"
       Requiere Ollama corriendo: https://ollama.com
       Modelos: ollama pull nomic-embed-text && ollama pull llama3.2

  3. anthropic — usa tu Anthropic API key + embeddings locales (HuggingFace)
       $env:MEM0_MODE = "anthropic"
       $env:ANTHROPIC_API_KEY = "sk-ant-..."

Uso:
    python scripts/memory/setup_mem0.py
"""

import os
import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

REPO_ROOT = Path(__file__).parent.parent.parent
SEEDS_DIR = REPO_ROOT / "project-memory" / "seeds"
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


def get_mode():
    return os.environ.get("MEM0_MODE", "managed").lower()


def get_mem0_client():
    try:
        from mem0 import Memory, MemoryClient
    except ImportError:
        print("ERROR: mem0ai no está instalado.")
        print("Ejecuta: pip install -r scripts/memory/requirements.txt")
        sys.exit(1)

    mode = get_mode()

    if mode == "managed":
        api_key = os.environ.get("MEM0_API_KEY", "")
        if not api_key:
            print("ERROR: MEM0_API_KEY no está configurada.")
            print("1. Regístrate gratis en https://app.mem0.ai")
            print("2. Copia tu API key")
            print("3. Configura: $env:MEM0_API_KEY = 'm0-...'")
            sys.exit(1)
        print("Modo: mem0 managed platform (cloud)")
        return MemoryClient(api_key=api_key), True

    elif mode == "ollama":
        print("Modo: Ollama local (sin API keys)")
        config = {
            "vector_store": {
                "provider": "chroma",
                "config": {
                    "collection_name": "fibradis",
                    "path": str(REPO_ROOT / "project-memory" / "chroma_db"),
                },
            },
            "llm": {
                "provider": "ollama",
                "config": {
                    "model": "llama3.2",
                    "ollama_base_url": "http://localhost:11434",
                },
            },
            "embedder": {
                "provider": "ollama",
                "config": {
                    "model": "nomic-embed-text",
                    "ollama_base_url": "http://localhost:11434",
                },
            },
            "history_db_path": DB_PATH,
        }
        return Memory.from_config(config), False

    elif mode == "anthropic":
        api_key = os.environ.get("ANTHROPIC_API_KEY", "")
        if not api_key:
            print("ERROR: ANTHROPIC_API_KEY no está configurada.")
            sys.exit(1)
        print("Modo: Anthropic LLM + embeddings HuggingFace (local)")
        config = {
            "vector_store": {
                "provider": "chroma",
                "config": {
                    "collection_name": "fibradis",
                    "path": str(REPO_ROOT / "project-memory" / "chroma_db"),
                },
            },
            "llm": {
                "provider": "anthropic",
                "config": {
                    "model": "claude-3-5-haiku-20241022",
                    "api_key": api_key,
                },
            },
            "embedder": {
                "provider": "huggingface",
                "config": {
                    "model": "multi-qa-MiniLM-L6-cos-v1",
                },
            },
            "history_db_path": DB_PATH,
        }
        return Memory.from_config(config), False

    else:
        print(f"ERROR: Modo '{mode}' no reconocido. Usa: managed, ollama, anthropic")
        sys.exit(1)


def load_seeds(client, is_managed: bool):
    seed_files = sorted(SEEDS_DIR.glob("*.md"))
    if not seed_files:
        print(f"No se encontraron archivos semilla en {SEEDS_DIR}")
        return

    print(f"\nCargando {len(seed_files)} archivos semilla en mem0...\n")

    for seed_file in seed_files:
        content = seed_file.read_text(encoding="utf-8")
        print(f"  Cargando: {seed_file.name}")
        if is_managed:
            client.add(
                content,
                user_id=AGENT_USER_ID,
                metadata={"source": "seed", "filename": seed_file.name},
            )
        else:
            client.add(
                content,
                user_id=AGENT_USER_ID,
                metadata={"source": "seed", "filename": seed_file.name},
            )

    print(f"\nOK: {len(seed_files)} archivos semilla cargados.")
    print("\nPrueba con:")
    print("  python scripts/memory/memory_cli.py search 'arquitectura modulos'")


if __name__ == "__main__":
    print("=== FIBRADIS mem0 Setup ===")
    print(f"Modo: {get_mode()}\n")
    client, is_managed = get_mem0_client()
    load_seeds(client, is_managed)
