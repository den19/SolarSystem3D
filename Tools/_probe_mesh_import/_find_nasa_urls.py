"""Probe NASA pages for glb/gltf download URLs."""
from __future__ import annotations

import re
import urllib.request

URLS = [
    "https://science.nasa.gov/resource/new-horizons-3d-model/",
    "https://science.nasa.gov/resource/juno-3d-model/",
    "https://science.nasa.gov/3d-resources/juno-a/",
    "https://science.nasa.gov/3d-resources/new-horizons/",
    "https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/n/",
]

UA = {"User-Agent": "Mozilla/5.0 (compatible; SolarSystemProbeImport/1.0)"}


def main() -> None:
    for url in URLS:
        print("===", url)
        try:
            req = urllib.request.Request(url, headers=UA)
            html = urllib.request.urlopen(req, timeout=45).read().decode("utf-8", "replace")
        except Exception as exc:  # noqa: BLE001
            print(" ERR", exc)
            continue
        found = set(re.findall(r"https?://[^\s\"'<>]+\.(?:glb|gltf|zip)", html, re.I))
        found |= set(re.findall(r"/[^\s\"'<>]+\.(?:glb|gltf)", html, re.I))
        for m in sorted(found):
            print(" ", m)


if __name__ == "__main__":
    main()
