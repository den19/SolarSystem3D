# Probe mesh import (batch half)

## Quick start

```powershell
python Tools/_probe_mesh_import/fetch_probe_meshes.py
# NASA only:
python Tools/_probe_mesh_import/fetch_probe_meshes.py --nasa-only
# After placing a manual GLB (e.g. Venera7-source.glb):
python Tools/_probe_mesh_import/fetch_probe_meshes.py --only Venera7
```

Then in Unity: **Solar System → Build Probe Mesh Prefabs**

## Sketchfab

Copy `Tools/sketchfab.local.ps1.example` → `Tools/sketchfab.local.ps1` and set `$SketchfabToken`,
or set env `SKETCHFAB_TOKEN`. On HTTP 429 the fetcher backs off then skips to procedural OBJ.

## Outputs

`Assets/Resources/ProbeMeshes/<Name>/` — OBJ, albedo PNGs, ATTRIBUTION.txt, mesh_info.json.
