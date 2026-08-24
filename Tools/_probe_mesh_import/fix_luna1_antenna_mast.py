"""
Close the floating central mast/tip gap on Luna-1 after Sketchfab decimation.

Loads Luna1.obj (preferably freshly converted from GLB), inserts a visible gold
mast cylinder from body into tip, exports body + mat_Mast as separate materials.
Preserves Luna1.obj.meta GUID.
"""
from __future__ import annotations

import json
import math
from pathlib import Path

import numpy as np
import trimesh

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Resources" / "ProbeMeshes" / "Luna1"
OBJ_PATH = OUT / "Luna1.obj"
ALBEDO_NAME = "Luna1_albedo.png"
# After FitToBounds(~0.34) radius 0.055 → ~0.019 world — readable on phone.
MAST_RADIUS = 0.055
AXIS_RXZ = 0.30
TIP_Y_MIN = 0.72


def read_existing_guid(meta_path: Path) -> str | None:
    if not meta_path.is_file():
        return None
    for line in meta_path.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            return line.split("guid: ", 1)[1].strip()
    return None


def spherical_uvs(verts: np.ndarray) -> np.ndarray:
    """Equirectangular UVs for probe body — u wraps longitude, v=0.5 is equator."""
    c = verts.mean(axis=0)
    rel = verts - c
    r = float(np.linalg.norm(rel, axis=1).mean())
    if r < 1e-6:
        r = 1.0
    u = 0.5 + np.arctan2(rel[:, 2], rel[:, 0]) / (2.0 * math.pi)
    v = 0.5 - np.arcsin(np.clip(rel[:, 1] / r, -1.0, 1.0)) / math.pi
    return np.column_stack([u, v])


def write_mesh_part(
    lines: list[str],
    mesh: trimesh.Trimesh,
    *,
    v_off: int,
    vt_off: int,
    vn_off: int,
    default_uv: tuple[float, float] | None = None,
    override_uvs: np.ndarray | None = None,
) -> tuple[int, int, int]:
    verts = np.asarray(mesh.vertices)
    norms = np.asarray(mesh.vertex_normals)
    uvs = override_uvs
    if uvs is None and default_uv is None and hasattr(mesh.visual, "uv") and mesh.visual.uv is not None:
        uvs = np.asarray(mesh.visual.uv)

    for v in verts:
        lines.append(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}")
    for n in norms:
        lines.append(f"vn {n[0]:.6f} {n[1]:.6f} {n[2]:.6f}")
    if uvs is not None and len(uvs) == len(verts):
        for uv in uvs:
            lines.append(f"vt {uv[0]:.6f} {uv[1]:.6f}")
    else:
        u, vv = default_uv if default_uv is not None else (0.5, 0.5)
        for _ in verts:
            lines.append(f"vt {u:.6f} {vv:.6f}")

    for face in mesh.faces:
        a, b, c = (int(i) + 1 for i in face)
        lines.append(
            "f "
            f"{a + v_off}/{a + vt_off}/{a + vn_off} "
            f"{b + v_off}/{b + vt_off}/{b + vn_off} "
            f"{c + v_off}/{c + vt_off}/{c + vn_off}"
        )
    return len(verts), len(verts), len(verts)


def export_obj(body: trimesh.Trimesh, mast: trimesh.Trimesh, albedo_file: str, out_obj: Path) -> None:
    mtl_name = out_obj.with_suffix(".mtl").name
    lines = [f"mtllib {mtl_name}", ""]
    mtl = [
        "newmtl mat_Luna1",
        "Kd 1.0 1.0 1.0",
        "Ka 0.0 0.0 0.0",
        "Ks 0.1 0.1 0.1",
        "d 1.0",
        f"map_Kd {albedo_file}",
        "",
        "newmtl mat_Mast",
        "Kd 0.92 0.78 0.32",
        "Ka 0.08 0.06 0.02",
        "Ks 0.45 0.38 0.18",
        "d 1.0",
        "",
    ]

    lines.append("o mat_Luna1")
    lines.append("usemtl mat_Luna1")
    v_off = vt_off = vn_off = 0
    body_uvs = spherical_uvs(np.asarray(body.vertices))
    dv, dvt, dvn = write_mesh_part(
        lines, body, v_off=v_off, vt_off=vt_off, vn_off=vn_off, override_uvs=body_uvs
    )
    v_off += dv
    vt_off += dvt
    vn_off += dvn

    lines.append("")
    lines.append("o mat_Mast")
    lines.append("usemtl mat_Mast")
    write_mesh_part(lines, mast, v_off=v_off, vt_off=vt_off, vn_off=vn_off, default_uv=(0.5, 0.5))

    out_obj.write_text("\n".join(lines) + "\n", encoding="utf-8")
    out_obj.with_suffix(".mtl").write_text("\n".join(mtl), encoding="utf-8")


def strip_previous_mast(mesh: trimesh.Trimesh) -> trimesh.Trimesh:
    """Drop a previously inserted thin/tall near-axis cylinder so re-runs are idempotent."""
    parts = mesh.split(only_watertight=False)
    kept: list[trimesh.Trimesh] = []
    for p in parts:
        e = p.extents
        # Our inserted mast: ~80 faces, tall in Y, thin in XZ.
        if 40 <= len(p.faces) <= 120 and e[1] > 0.12 and max(e[0], e[2]) < 0.15:
            c = p.centroid
            if c[1] > 0.55:
                continue
        kept.append(p)
    if not kept:
        return mesh
    if len(kept) == len(parts):
        return mesh
    return trimesh.util.concatenate(kept)


def tip_axis(verts: np.ndarray) -> tuple[float, float, float, float]:
    """Return ax, az, tip_bottom, tip_top from high-Y near-axis tip cluster."""
    # First pass: top percentile to locate tip xz.
    hi = verts[verts[:, 1] > np.percentile(verts[:, 1], 96)]
    if len(hi) < 4:
        hi = verts[verts[:, 1] > np.percentile(verts[:, 1], 90)]
    ax = float(np.median(hi[:, 0]))
    az = float(np.median(hi[:, 2]))
    rxz = np.hypot(verts[:, 0] - ax, verts[:, 2] - az)
    tip = (verts[:, 1] > TIP_Y_MIN) & (rxz < AXIS_RXZ)
    if tip.sum() < 4:
        tip = (verts[:, 1] > np.percentile(verts[:, 1], 97)) & (rxz < AXIS_RXZ * 1.4)
    if tip.sum() < 1:
        raise RuntimeError("Could not find tip cluster")
    # Refine axis to tip median.
    ax = float(np.median(verts[tip, 0]))
    az = float(np.median(verts[tip, 2]))
    tip_bottom = float(verts[tip, 1].min())
    tip_top = float(verts[tip, 1].max())
    return ax, az, tip_bottom, tip_top


def body_top_under_tip(verts: np.ndarray, ax: float, az: float, tip_bottom: float) -> float:
    c = verts.mean(axis=0)
    rxz = np.hypot(verts[:, 0] - ax, verts[:, 2] - az)
    core = (np.abs(verts[:, 0] - c[0]) < 1.05) & (np.abs(verts[:, 2] - c[2]) < 1.05)
    body = core & (verts[:, 1] < tip_bottom - 0.06) & (verts[:, 1] > 0.15)
    near = body & (rxz < 0.55)
    if near.sum() < 8:
        near = body & (rxz < 0.85)
    if near.sum() < 1:
        raise RuntimeError("Could not estimate body top under tip")
    return float(verts[near, 1].max())


def make_mast(ax: float, az: float, y0: float, y1: float) -> trimesh.Trimesh:
    height = max(0.05, y1 - y0)
    mast = trimesh.creation.cylinder(radius=MAST_RADIUS, height=height, sections=24)
    rot = trimesh.transformations.rotation_matrix(math.pi / 2.0, [1.0, 0.0, 0.0])
    mast.apply_transform(rot)
    mast.apply_translation([ax, (y0 + y1) * 0.5, az])
    return mast


def update_mesh_info(mesh: trimesh.Trimesh, antenna_local: list[float]) -> None:
    bounds = mesh.bounds
    info = {
        "bounds_min": bounds[0].tolist(),
        "bounds_max": bounds[1].tolist(),
        "extents": mesh.extents.tolist(),
        "center": mesh.centroid.tolist(),
        "antenna_local": antenna_local,
        "suggested_visual_euler_xyz": [0.0, 0.0, 0.0],
        "source": "Imported GLB Luna1-source.glb + antenna mast patch",
        "faces": int(len(mesh.faces)),
        "verts": int(len(mesh.vertices)),
    }
    (OUT / "mesh_info.json").write_text(json.dumps(info, indent=2), encoding="utf-8")
    (Path(__file__).resolve().parent / "Luna1_antenna_local.json").write_text(
        json.dumps(
            {"antenna_local": antenna_local, "visual_euler": [0.0, 0.0, 0.0], "faces": info["faces"]},
            indent=2,
        ),
        encoding="utf-8",
    )


def main() -> int:
    if not OBJ_PATH.is_file():
        raise SystemExit(f"Missing {OBJ_PATH}")

    mesh = trimesh.load(str(OBJ_PATH), force="mesh")
    if not isinstance(mesh, trimesh.Trimesh):
        raise SystemExit("Luna1.obj did not load as a single mesh")

    body = strip_previous_mast(mesh)
    verts = np.asarray(body.vertices)
    ax, az, tip_bottom, tip_top = tip_axis(verts)
    body_top = body_top_under_tip(verts, ax, az, tip_bottom)
    gap = tip_bottom - body_top
    print(
        f"axis=({ax:.4f}, {az:.4f}) body_top={body_top:.4f} "
        f"tip_bottom={tip_bottom:.4f} tip_top={tip_top:.4f} gap={gap:.4f}"
    )

    # Always rebuild mast: sink into body and reach tip so the joint reads solid.
    y0 = min(body_top - 0.08, tip_bottom - 0.12)
    y1 = tip_top + 0.02
    mast = make_mast(ax, az, y0, y1)
    print(f"Inserting mast y=[{y0:.4f},{y1:.4f}] height={y1 - y0:.4f} radius={MAST_RADIUS}")

    combined = trimesh.util.concatenate([body, mast])
    tip_verts = combined.vertices[
        (np.hypot(combined.vertices[:, 0] - ax, combined.vertices[:, 2] - az) < AXIS_RXZ)
        & (combined.vertices[:, 1] > tip_top - 0.1)
    ]
    if len(tip_verts) > 0:
        antenna_local = [
            float(np.median(tip_verts[:, 0])),
            float(np.percentile(tip_verts[:, 1], 90)),
            float(np.median(tip_verts[:, 2])),
        ]
    else:
        antenna_local = [ax, tip_top, az]

    albedo = ALBEDO_NAME if (OUT / ALBEDO_NAME).is_file() else "Luna1_albedo.png"
    guid = read_existing_guid(OBJ_PATH.with_suffix(".obj.meta")) or "2920581caa5c4e8f8ac4892939e98e79"
    export_obj(body, mast, albedo, OBJ_PATH)
    update_mesh_info(combined, antenna_local)

    meta = OBJ_PATH.with_suffix(".obj.meta")
    if meta.is_file():
        lines = meta.read_text(encoding="utf-8").splitlines()
        for i, line in enumerate(lines):
            if line.startswith("guid: "):
                lines[i] = f"guid: {guid}"
                break
        meta.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print("Wrote", OBJ_PATH, "faces", len(combined.faces), "antenna_local", antenna_local)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
