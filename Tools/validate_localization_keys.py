#!/usr/bin/env python3
"""Verify all localization JSON files have identical key sets."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from generate_localization import parse_unity_json

LANG_DIR = Path(__file__).resolve().parents[1] / "Assets" / "Resources" / "Languages"
FILES = ["english.json", "russian.json", "chinese.json", "vietnamese.json"]


def main():
    parsed = {}
    for name in FILES:
        path = LANG_DIR / name
        if not path.exists():
            print(f"MISSING: {name}")
            sys.exit(1)
        parsed[name] = set(parse_unity_json(path).keys())

    reference = parsed["english.json"]
    ok = True
    for name, keys in parsed.items():
        missing = reference - keys
        extra = keys - reference
        if missing or extra:
            ok = False
            print(f"{name}: {len(keys)} keys — missing {len(missing)}, extra {len(extra)}")
            if missing:
                print("  missing:", sorted(missing)[:10], "...")
            if extra:
                print("  extra:", sorted(extra)[:10], "...")
        else:
            print(f"{name}: OK ({len(keys)} keys)")

    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
