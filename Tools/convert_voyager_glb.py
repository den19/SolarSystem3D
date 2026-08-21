"""
Convert NASA Voyager GLB to Unity OBJ+PNG under Assets/Resources/ProbeMeshes/Voyager1/.

Primary: Voyager Probe (B) — skipped when Draco mesh positions cannot be decoded.
Fallback: VTAD Voyager.glb from https://science.nasa.gov/resource/voyager-3d-model/

Usage:
  python Tools/convert_voyager_glb.py
  python Tools/convert_voyager_glb.py --src Tools/_voyager_import/Voyager-VTAD.glb
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPORT_DIR = ROOT / "Tools" / "_voyager_import"
CONVERT = IMPORT_DIR / "convert_voyager_to_obj.py"
VTAD_URL = "https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/v/Voyager.glb"
PROBE_B_URL = (
    "https://assets.science.nasa.gov/content/dam/science/cds/3d/resources/model/"
    "voyager-probe-(b)/Voyager%20Probe%20(B).glb"
)


def download(url: str, dest: Path) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    print(f"Downloading {url}")
    try:
        import urllib.request

        urllib.request.urlretrieve(url, dest)
    except Exception as exc:
        raise SystemExit(f"Download failed: {exc}") from exc
    print(f"Saved {dest} ({dest.stat().st_size} bytes)")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--src", type=Path, help="Existing .glb to convert")
    parser.add_argument("--skip-download", action="store_true")
    args = parser.parse_args()

    IMPORT_DIR.mkdir(parents=True, exist_ok=True)
    src = args.src
    if src is None:
        vtad = IMPORT_DIR / "Voyager-VTAD.glb"
        probe_b = IMPORT_DIR / "Voyager-Probe-B.glb"
        if not args.skip_download:
            if not probe_b.exists():
                download(PROBE_B_URL, probe_b)
            if not vtad.exists():
                download(VTAD_URL, vtad)
        # Prefer VTAD: Probe B uses Draco that trimesh cannot decompress here.
        src = vtad if vtad.exists() else probe_b

    if not src.exists():
        raise SystemExit(f"Source GLB not found: {src}")

    # convert_voyager_to_obj.py expects Voyager-VTAD.glb beside itself.
    expected = IMPORT_DIR / "Voyager-VTAD.glb"
    if src.resolve() != expected.resolve():
        shutil.copy2(src, expected)

    if not CONVERT.exists():
        raise SystemExit(f"Missing converter: {CONVERT}")

    print(f"Converting {src} via {CONVERT}")
    return subprocess.call([sys.executable, str(CONVERT)])


if __name__ == "__main__":
    raise SystemExit(main())
