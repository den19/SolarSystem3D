# Probe mesh import

## Quick start

```powershell
python Tools/_probe_mesh_import/fetch_probe_meshes.py
# NASA only:
python Tools/_probe_mesh_import/fetch_probe_meshes.py --nasa-only
# Remaining / one probe after placing GLB:
python Tools/_probe_mesh_import/fetch_probe_meshes.py --only Luna16,Tianwen1,Change5,Akatsuki,Chandrayaan3
```

Then in Unity: **Solar System → Build Probe Mesh Prefabs**

## Sketchfab / 429

Copy `Tools/sketchfab.local.ps1.example` → `Tools/sketchfab.local.ps1` and set `$SketchfabToken`,
or set env `SKETCHFAB_TOKEN`. On HTTP 429 the fetcher backs off (1→2→5→10 min) then skips to
procedural OBJ / next probe. Manual: drop `*-source.glb` next to this README and re-run `--only`.

## Outputs

`Assets/Resources/ProbeMeshes/<Name>/` — OBJ, albedo PNGs, ATTRIBUTION.txt, mesh_info.json.
All presets except Custom ship a mesh pack (NASA GLB or procedural stand-in until a CC GLB is provided).
