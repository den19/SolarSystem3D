import trimesh
from pathlib import Path


def inspect(path: Path) -> None:
    print("===", path.name, "===")
    scene = trimesh.load(str(path), force="scene")
    print("type", type(scene))
    if isinstance(scene, trimesh.Scene):
        print("graph nodes", len(scene.graph.nodes))
        print("geometry count", len(scene.geometry))
        print("geometry keys", list(scene.geometry.keys())[:40])
        for node in list(scene.graph.nodes_geometry)[:60]:
            print(" geo node:", node)
        try:
            print("bounds", scene.bounds)
            print("extents", scene.extents)
        except Exception as e:
            print("bounds err", e)
        for name, geom in list(scene.geometry.items())[:15]:
            print(
                f"  mesh {name}: faces={len(geom.faces)} verts={len(geom.vertices)} "
                f"visual={type(geom.visual)}"
            )
            if hasattr(geom.visual, "material"):
                mat = geom.visual.material
                print("   material", type(mat), getattr(mat, "name", None))
                for attr in ("baseColorTexture", "image", "main_color"):
                    if hasattr(mat, attr):
                        v = getattr(mat, attr)
                        print(
                            f"   {attr}",
                            type(v),
                            getattr(v, "size", None) if v is not None else None,
                        )
    else:
        print(
            "single mesh faces",
            len(scene.faces),
            "verts",
            len(scene.vertices),
            "extents",
            scene.extents,
        )


base = Path(__file__).resolve().parent
for name in [
    "Voyager-Probe-B.glb",
    "Voyager-VTAD.glb",
    "Voyager-Probe-B-antenna.glb",
]:
    try:
        inspect(base / name)
    except Exception as e:
        print("FAIL", name, e)
