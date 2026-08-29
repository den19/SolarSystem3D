# Probe mesh import

## Quick start

Python deps: `pip install trimesh numpy pillow` and **`pip install fast_simplification`** (required for GLB decimation; without it meshes become broken triangle soup).

```powershell
python Tools/_probe_mesh_import/fetch_probe_meshes.py
# NASA only:
python Tools/_probe_mesh_import/fetch_probe_meshes.py --nasa-only
# Remaining / one probe after placing GLB:
python Tools/_probe_mesh_import/fetch_probe_meshes.py --only Luna16,Tianwen1,Change5,Akatsuki,Chandrayaan3
```

Then in Unity (Editor closed for batch):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "<repo>" `
  -executeMethod ProbeMeshPrefabBuilder.BuildAllAndVerify `
  -logFile "Temp\probe_prefab_batch.log"
```

Or menu: **Solar System → Build Probe Mesh Prefabs**

## Picker visibility (in-game list)

Only **6** mesh packs are shown in the probe picker (`ProbeModelCatalog.ShowInPicker`), **display order**:

| # | Folder | Source quality |
|---|--------|----------------|
| 1 | Luna1 | Sketchfab GLB (`Luna1-source.glb`; requires `fast_simplification`) |
| 2 | Mars3 | Sketchfab CC BY (separate `Mars3PrefabBuilder`) |
| 3 | Venera7 | Sketchfab GLB (`Venera7-source.glb`) |
| 4 | Voyager1 | NASA VTAD (separate `Voyager1PrefabBuilder`) |
| 5 | NewHorizons | NASA VTAD |
| 6 | Juno | NASA VTAD |

**Default model** on first launch / Reset: **Luna 1** (`ProbeModelCatalog.GetDefaultPickerModel()`). Hidden pref on load → remap to Luna 1.

**Hidden** until a real GLB replaces procedural stand-in: Luna16, Change4, Change5, Tianwen1, Hayabusa2, Akatsuki, Chandrayaan3, Custom. Assets stay under `Assets/Resources/ProbeMeshes/`; re-enable by setting `ShowInPicker = true`.

## Sketchfab / 429

Copy `Tools/sketchfab.local.ps1.example` → `Tools/sketchfab.local.ps1` and set `$SketchfabToken`,
or set env `SKETCHFAB_TOKEN`. On HTTP 429 the fetcher backs off (1→2→5→10 min) then skips to
procedural OBJ / next probe. Manual: drop `*-source.glb` next to this README and re-run `--only`.

## Outputs

`Assets/Resources/ProbeMeshes/<Name>/` — OBJ, albedo PNGs, ATTRIBUTION.txt, mesh_info.json.
All presets except Custom ship a mesh pack (NASA GLB or procedural stand-in until a CC GLB is provided).
