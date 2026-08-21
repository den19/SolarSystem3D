"""
Download probe GLBs (NASA first), then convert to Unity OBJ packs.

Usage:
  python Tools/_probe_mesh_import/fetch_probe_meshes.py
  python Tools/_probe_mesh_import/fetch_probe_meshes.py --nasa-only
  python Tools/_probe_mesh_import/fetch_probe_meshes.py --skip-existing
  python Tools/_probe_mesh_import/fetch_probe_meshes.py --only NewHorizons,Juno

Sketchfab downloads require SKETCHFAB_TOKEN (or Tools/sketchfab.local.ps1 $SketchfabToken).
On HTTP 429: exponential backoff then skip that probe and continue.
Missing Sketchfab auth / 429 → procedural OBJ approximation for non-NASA targets.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
IMPORT = Path(__file__).resolve().parent
CONVERT = IMPORT / "convert_glb_to_probe_obj.py"
UA = {
    "User-Agent": "Mozilla/5.0 (compatible; SolarSystemProbeImport/1.0; +local-dev)",
    "Accept": "*/*",
}

PROBES = [
    {
        "name": "NewHorizons",
        "kind": "nasa",
        "url": "https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/n/New_Horizons.glb",
        "glb": "NewHorizons-source.glb",
        "source_note": "NASA VTAD New Horizons GLB",
    },
    {
        "name": "Juno",
        "kind": "nasa",
        "url": "https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/j/Juno.glb",
        "glb": "Juno-source.glb",
        "source_note": "NASA VTAD Juno GLB",
    },
    {
        "name": "Venera7",
        "kind": "sketchfab",
        "uid": "fd464eedd93a41c98d2608faeb871cd0",
        "glb": "Venera7-source.glb",
        "procedural": "Venera7",
        "page": "https://sketchfab.com/3d-models/venera-7-fd464eedd93a41c98d2608faeb871cd0",
    },
    {
        "name": "Luna1",
        "kind": "sketchfab",
        "uid": "1b7feef6a0614837b21790e219e68ab2",
        "glb": "Luna1-source.glb",
        "procedural": "Luna1",
        "page": "https://sketchfab.com/3d-models/soviet-space-satellite-moon-1-1b7feef6a0614837b21790e219e68ab2",
    },
    {
        "name": "Hayabusa2",
        "kind": "sketchfab",
        "uid": None,  # filled if token search finds one; else procedural
        "glb": "Hayabusa2-source.glb",
        "procedural": "Hayabusa2",
        "page": "https://sketchfab.com/search?q=hayabusa2&type=models&features=downloadable",
    },
    {
        "name": "Change4",
        "kind": "sketchfab",
        "uid": "d04a86f43d4549328a19a30da3e7509f",  # Chang'e 3 as closest free lander silhouette if 4 unavailable
        "glb": "Change4-source.glb",
        "procedural": "Change4",
        "page": "https://sketchfab.com/3d-models/change-3-d04a86f43d4549328a19a30da3e7509f",
    },
]

BACKOFF_SECONDS = [60, 120, 300, 600]


def load_sketchfab_token() -> str | None:
    env = os.environ.get("SKETCHFAB_TOKEN") or os.environ.get("SKETCHFAB_API_TOKEN")
    if env:
        return env.strip()
    local = ROOT / "Tools" / "sketchfab.local.ps1"
    if local.exists():
        text = local.read_text(encoding="utf-8", errors="replace")
        m = re.search(r"\$SketchfabToken\s*=\s*['\"]([^'\"]+)['\"]", text)
        if m:
            return m.group(1).strip()
    return None


def download_url(url: str, dest: Path, *, token: str | None = None) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    headers = dict(UA)
    if token:
        headers["Authorization"] = f"Token {token}"
    req = urllib.request.Request(url, headers=headers)
    print(f"Downloading {url}")
    with urllib.request.urlopen(req, timeout=180) as resp, open(dest, "wb") as f:
        while True:
            chunk = resp.read(1024 * 256)
            if not chunk:
                break
            f.write(chunk)
    print(f"Saved {dest} ({dest.stat().st_size} bytes)")


def download_with_backoff(url: str, dest: Path, *, token: str | None = None) -> bool:
    for attempt, wait in enumerate([0] + BACKOFF_SECONDS):
        if wait:
            print(f"Backoff {wait}s (attempt {attempt + 1}) after rate limit…")
            time.sleep(wait)
        try:
            download_url(url, dest, token=token)
            return True
        except urllib.error.HTTPError as exc:
            print(f"HTTP {exc.code} for {url}")
            if exc.code == 429:
                continue
            if exc.code in (401, 403):
                print("Auth/forbidden — skip Sketchfab download for this probe.")
                return False
            return False
        except Exception as exc:  # noqa: BLE001
            print("Download error:", exc)
            return False
    print("Gave up after 429 backoffs; skipping.")
    return False


def sketchfab_download_glb(uid: str, dest: Path, token: str) -> bool:
    """Use Sketchfab download API; extract first .glb from zip."""
    api = f"https://api.sketchfab.com/v3/models/{uid}/download"
    headers = dict(UA)
    headers["Authorization"] = f"Token {token}"
    req = urllib.request.Request(api, headers=headers)
    for attempt, wait in enumerate([0] + BACKOFF_SECONDS):
        if wait:
            print(f"Sketchfab backoff {wait}s…")
            time.sleep(wait)
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:
                data = json.loads(resp.read().decode("utf-8"))
            # Prefer gltf zip URL.
            url = None
            for key in ("gltf", "glb", "source"):
                node = data.get(key) or {}
                if isinstance(node, dict) and node.get("url"):
                    url = node["url"]
                    break
            if not url:
                print("No download URL in Sketchfab response:", list(data.keys()))
                return False
            zip_path = dest.with_suffix(".zip")
            if not download_with_backoff(url, zip_path):
                return False
            with zipfile.ZipFile(zip_path, "r") as zf:
                glbs = [n for n in zf.namelist() if n.lower().endswith(".glb")]
                gltfs = [n for n in zf.namelist() if n.lower().endswith(".gltf")]
                if glbs:
                    with zf.open(glbs[0]) as src, open(dest, "wb") as out:
                        out.write(src.read())
                elif gltfs:
                    # Unpack whole zip beside dest and point to scene.gltf — trimesh can load gltf folder.
                    unpack = dest.parent / f"{dest.stem}_unpacked"
                    if unpack.exists():
                        import shutil

                        shutil.rmtree(unpack)
                    zf.extractall(unpack)
                    # Find root gltf
                    found = list(unpack.rglob("*.gltf"))
                    if not found:
                        print("No gltf in zip")
                        return False
                    # Convert via trimesh load path by copying as .gltf next to expected — use convert --src gltf
                    dest = dest.with_suffix(".gltf")
                    import shutil

                    shutil.copy2(found[0], dest)
                    print("Extracted glTF to", dest)
                    zip_path.unlink(missing_ok=True)
                    return True
                else:
                    print("Zip had no glb/gltf")
                    return False
            zip_path.unlink(missing_ok=True)
            print("Extracted GLB to", dest)
            return True
        except urllib.error.HTTPError as exc:
            print(f"Sketchfab API HTTP {exc.code}")
            if exc.code == 429:
                continue
            return False
        except Exception as exc:  # noqa: BLE001
            print("Sketchfab error:", exc)
            return False
    return False


def obj_exists(name: str) -> bool:
    return (ROOT / "Assets" / "Resources" / "ProbeMeshes" / name / f"{name}.obj").exists()


def run_convert(name: str, src: Path | None, procedural: str | None, source_note: str | None) -> None:
    cmd = [sys.executable, str(CONVERT), "--out-name", name]
    if src is not None and src.exists():
        cmd += ["--src", str(src)]
        if source_note:
            cmd += ["--source-note", source_note]
    elif procedural:
        cmd += ["--procedural", procedural]
    else:
        raise SystemExit(f"No source for {name}")
    print("Running", " ".join(cmd))
    subprocess.check_call(cmd)


def process_probe(probe: dict, *, nasa_only: bool, skip_existing: bool, token: str | None, pause_s: float) -> str:
    name = probe["name"]
    if skip_existing and obj_exists(name):
        print(f"[skip-existing] {name}")
        return "skipped_existing"

    glb_path = IMPORT / probe["glb"]

    if probe["kind"] == "nasa":
        if not glb_path.exists():
            ok = download_with_backoff(probe["url"], glb_path)
            if not ok:
                print(f"[FAIL] NASA download {name}")
                return "failed"
        run_convert(name, glb_path, None, probe.get("source_note"))
        return "ok"

    if nasa_only:
        print(f"[nasa-only] skip {name}")
        return "skipped_nasa_only"

    # Prefer pre-placed local GLB (manual download).
    if glb_path.exists():
        run_convert(name, glb_path, None, f"Local GLB {glb_path.name}")
        return "ok"

    uid = probe.get("uid")
    if uid and token:
        if pause_s > 0:
            print(f"Pause {pause_s:.0f}s before Sketchfab {name}…")
            time.sleep(pause_s)
        glb_try = IMPORT / probe["glb"]
        if sketchfab_download_glb(uid, glb_try, token):
            # May have written .gltf
            src = glb_try if glb_try.exists() else glb_try.with_suffix(".gltf")
            run_convert(name, src, None, f"Sketchfab {uid}")
            return "ok"
        print(f"[429/auth] Sketchfab failed for {name}; using procedural")
    else:
        reason = "no SKETCHFAB_TOKEN" if not token else "no uid"
        print(f"[{reason}] {name} -> procedural")

    run_convert(name, None, probe.get("procedural"), None)
    # Write MANUAL note
    note = IMPORT / f"{name}_MANUAL_DOWNLOAD.txt"
    note.write_text(
        f"Place {probe['glb']} here (or set SKETCHFAB_TOKEN) then re-run:\n"
        f"  python Tools/_probe_mesh_import/fetch_probe_meshes.py --only {name}\n"
        f"Page: {probe.get('page', '')}\n",
        encoding="utf-8",
    )
    return "procedural"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--nasa-only", action="store_true")
    parser.add_argument("--skip-existing", action="store_true")
    parser.add_argument("--only", help="Comma-separated probe names")
    parser.add_argument("--sketchfab-pause", type=float, default=45.0, help="Seconds between Sketchfab calls")
    args = parser.parse_args()

    only = None
    if args.only:
        only = {x.strip() for x in args.only.split(",") if x.strip()}

    token = load_sketchfab_token()
    if token:
        print("Sketchfab token: present")
    else:
        print("Sketchfab token: absent (NASA + procedural / local GLB only)")

    results = {}
    for probe in PROBES:
        if only and probe["name"] not in only:
            continue
        results[probe["name"]] = process_probe(
            probe,
            nasa_only=args.nasa_only,
            skip_existing=args.skip_existing,
            token=token,
            pause_s=args.sketchfab_pause,
        )

    print("=== Summary ===")
    for k, v in results.items():
        print(f"  {k}: {v}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
