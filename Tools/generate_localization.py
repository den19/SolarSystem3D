#!/usr/bin/env python3
"""Parse Unity localization JSON and generate chinese/vietnamese/uzbek translations."""
import json
import re
import sys
import time
from pathlib import Path

try:
    from deep_translator import GoogleTranslator
except ImportError:
    print("Install: pip install deep-translator")
    sys.exit(1)

ROOT = Path(__file__).resolve().parents[1]
LANG_DIR = ROOT / "Assets" / "Resources" / "Languages"
ENGLISH = LANG_DIR / "english.json"
CHUNK_SIZE = 4500


def parse_unity_json(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    if not text.startswith("{"):
        raise ValueError("Expected object")
    text = text[1:]
    if text.endswith("}"):
        text = text[:-1]

    result: dict[str, str] = {}
    i = 0
    n = len(text)

    while i < n:
        while i < n and text[i] in " \t\r\n,":
            i += 1
        if i >= n:
            break
        if text[i] != '"':
            raise ValueError(f"Expected key quote at {i}: {text[i:i+20]!r}")
        i += 1
        key_start = i
        while i < n and text[i] != '"':
            i += 1
        key = text[key_start:i]
        i += 1
        if i >= n or text[i] != ':':
            raise ValueError(f"Expected colon after key {key}")
        i += 1
        if i >= n or text[i] != '"':
            raise ValueError(f"Expected value quote for {key}")
        i += 1
        val_start = i
        while i < n:
            ch = text[i]
            if ch == "\\" and i + 1 < n:
                i += 2
                continue
            if ch == '"':
                j = i + 1
                while j < n and text[j] in " \t\r\n":
                    j += 1
                if j >= n or text[j] == "," or text[j] == "}":
                    break
            i += 1
        value = text[val_start:i]
        value = value.replace('\\"', '"').replace("\\n", "\n").replace("\\r", "\r").replace("\\\\", "\\")
        result[key] = value
        i += 1

    return result


def serialize_unity_json(data: dict[str, str], key_order: list[str]) -> str:
    parts = ["{"]
    for idx, key in enumerate(key_order):
        val = data[key]
        val = val.replace("\\", "\\\\").replace('"', '\\"')
        parts.append(f'"{key}":"{val}"')
        if idx < len(key_order) - 1:
            parts.append(",")
    parts.append("}")
    return "".join(parts)


def translate_text(text: str, target: str) -> str:
    if not text.strip():
        return text
    translator = GoogleTranslator(source="en", target=target)
    if len(text) <= CHUNK_SIZE:
        return translator.translate(text)

    chunks: list[str] = []
    start = 0
    while start < len(text):
        end = min(start + CHUNK_SIZE, len(text))
        if end < len(text):
            split = text.rfind("\n", start, end)
            if split <= start:
                split = text.rfind(" ", start, end)
            if split > start:
                end = split + 1
        piece = text[start:end]
        chunks.append(translator.translate(piece))
        start = end
        time.sleep(0.3)
    return "".join(chunks)


def build_language(target_code: str, out_name: str) -> None:
    en = parse_unity_json(ENGLISH)
    keys = list(en.keys())
    print(f"Parsed {len(keys)} keys from english.json")

    translated: dict[str, str] = {}
    for i, key in enumerate(keys):
        print(f"[{i + 1}/{len(keys)}] {key} ({len(en[key])} chars)")
        try:
            translated[key] = translate_text(en[key], target_code)
        except Exception as e:
            print(f"  Warning: {e}, keeping English for {key}")
            translated[key] = en[key]
        time.sleep(0.5)

    out_path = LANG_DIR / out_name
    out_path.write_text(serialize_unity_json(translated, keys), encoding="utf-8")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


def main():
    if len(sys.argv) < 2:
        print("Usage: generate_localization.py chinese|vietnamese|uzbek|both|all")
        sys.exit(1)
    cmd = sys.argv[1].lower()
    if cmd in ("chinese", "both", "all"):
        build_language("zh-CN", "chinese.json")
    if cmd in ("vietnamese", "both", "all"):
        build_language("vi", "vietnamese.json")
    if cmd in ("uzbek", "all"):
        build_language("uz", "uzbek.json")


if __name__ == "__main__":
    main()
