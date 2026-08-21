"""Try candidate NASA GLB URLs with HEAD/GET."""
from __future__ import annotations

import urllib.error
import urllib.request

CANDIDATES = [
    "https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/n/NewHorizons.glb",
    "https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/n/New_Horizons.glb",
    "https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/j/Juno.glb",
    "https://assets.science.nasa.gov/content/dam/science/cds/3d/resources/model/new-horizons/NewHorizons.glb",
    "https://assets.science.nasa.gov/content/dam/science/cds/3d/resources/model/juno/Juno.glb",
    "https://assets.science.nasa.gov/content/dam/science/cds/3d/resources/model/juno-(a)/Juno%20(A).glb",
    "https://assets.science.nasa.gov/content/dam/science/cds/3d/resources/model/juno-a/Juno%20(A).glb",
    "https://nasa3d.arc.nasa.gov/shared_assets/models/juno/juno.glb",
    "https://nasa3d.arc.nasa.gov/shared_assets/models/newhorizons/newhorizons.glb",
]

UA = {"User-Agent": "Mozilla/5.0 (compatible; SolarSystemProbeImport/1.0)"}


def check(url: str) -> None:
    req = urllib.request.Request(url, headers=UA, method="HEAD")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            print(resp.status, resp.headers.get("Content-Length"), url)
    except urllib.error.HTTPError as exc:
        print(exc.code, url)
    except Exception as exc:  # noqa: BLE001
        print("ERR", type(exc).__name__, exc, url)


def main() -> None:
    import re

    pages = [
        "https://science.nasa.gov/resource/new-horizons-3d-model/",
        "https://science.nasa.gov/resource/juno-3d-model/",
        "https://science.nasa.gov/3d-resources/juno-a/",
    ]
    for page in pages:
        print("=== PAGE", page)
        try:
            req = urllib.request.Request(page, headers=UA)
            html = urllib.request.urlopen(req, timeout=45).read().decode("utf-8", "replace")
            for m in sorted(set(re.findall(r"https?://[^\s\"'<>]+?\.(?:glb|gltf|zip)", html, re.I))):
                print(" ", m)
            for m in sorted(set(re.findall(r"/[^\s\"'<>]+?\.(?:glb|gltf)", html, re.I)))[:30]:
                print("  rel", m)
        except Exception as exc:  # noqa: BLE001
            print(" page ERR", exc)

    print("=== HEAD")
    for u in CANDIDATES:
        check(u)


if __name__ == "__main__":
    main()
