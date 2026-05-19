#!/usr/bin/env python3
"""
resolve_customization.py — BMad skill customization resolver

Reads base → team → user TOML override files and outputs the merged result
for the requested key block as JSON.

Usage:
    python resolve_customization.py --skill <skill-root> --key <block-key>

Merge rules (per BMad spec):
    - Scalars: last writer wins (user > team > base)
    - Tables (dicts): deep-merge
    - Arrays of tables with 'code' or 'id': replace matching entries, append new ones
    - All other arrays: append (base + team + user)
"""

import argparse
import json
import sys
from pathlib import Path

try:
    import tomllib
except ImportError:
    try:
        import tomli as tomllib  # pip install tomli
    except ImportError:
        print("Error: requires Python 3.11+ or 'tomli' package", file=sys.stderr)
        sys.exit(1)


def load_toml(path: Path) -> dict:
    if not path.exists():
        return {}
    with open(path, "rb") as f:
        return tomllib.load(f)


def merge_arrays(base: list, override: list) -> list:
    if not override:
        return list(base)
    if not base:
        return list(override)

    all_items = base + override
    if all(isinstance(item, dict) for item in all_items):
        for field in ("code", "id"):
            if all(field in item for item in all_items):
                result = list(base)
                for ov_item in override:
                    key_val = ov_item.get(field)
                    replaced = False
                    for i, base_item in enumerate(result):
                        if base_item.get(field) == key_val:
                            result[i] = ov_item
                            replaced = True
                            break
                    if not replaced:
                        result.append(ov_item)
                return result

    return base + override


def deep_merge(base: dict, override: dict) -> dict:
    result = dict(base)
    for key, ov_val in override.items():
        if key not in result:
            result[key] = ov_val
        else:
            base_val = result[key]
            if isinstance(base_val, dict) and isinstance(ov_val, dict):
                result[key] = deep_merge(base_val, ov_val)
            elif isinstance(base_val, list) and isinstance(ov_val, list):
                result[key] = merge_arrays(base_val, ov_val)
            else:
                result[key] = ov_val  # scalar: override wins
    return result


def main():
    parser = argparse.ArgumentParser(description="Resolve BMad skill customization")
    parser.add_argument("--skill", required=True, help="Absolute path to skill root directory")
    parser.add_argument("--key", required=True, help="Config block to output (e.g. 'workflow')")
    args = parser.parse_args()

    skill_root = Path(args.skill)
    skill_name = skill_root.name

    # Skill lives at {project-root}/.claude/skills/{skill-name}
    project_root = skill_root.parent.parent.parent

    base = load_toml(skill_root / "customize.toml")
    team = load_toml(project_root / "_bmad" / "custom" / f"{skill_name}.toml")
    user = load_toml(project_root / "_bmad" / "custom" / f"{skill_name}.user.toml")

    merged = deep_merge(deep_merge(base, team), user)
    print(json.dumps(merged.get(args.key, {}), indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
