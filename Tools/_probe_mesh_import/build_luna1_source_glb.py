"""
Build Luna1-source.glb from Sketchfab viewer textures + historically shaped mesh.

When Luna1-source.glb is missing, run this before convert_glb_to_probe_obj.py.
Textures are fetched from the public Sketchfab embed (no API token / no 429).
"""
from __future__ import annotations

import html as html_lib
import json
import math
import re
import urllib.parse
import urllib.request
import zipfile
from pathlib import Path

import numpy as np
import trimesh
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
IMPORT = Path(__file__).resolve().parent
OUT_GLB = IMPORT / "Luna1-source.glb"
TEXTURE_DIR = IMPORT / "Luna1_viewer_textures"
UID = "1b7feef6a0614837b21790e219e68ab2"
EMBED = f"https://sketchfab.com/models/{UID}/embed?autostart=1"


def fetch_viewer_textures() -> list[Path]:
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    req = urllib.request.Request(EMBED, headers={"User-Agent": "Mozilla/5.0"})
    page = html_lib.unescape(urllib.request.urlopen(req, timeout=30).read().decode("utf-8", "replace"))
    start = page.find('"files":')
    chunk = page[start : start + 2500] if start >= 0 else page
    p_match = re.search(r'"p"\s*:\s*(\[\{[^\]]+\}\])', chunk)
    params = json.loads(p_match.group(1)) if p_match else []
    qs = urllib.parse.urlencode({"v": params[0]["v"], "b": params[0]["b"]}) if params else ""

    urls = sorted(
        set(
            u
            for u in re.findall(
                r"https://media\.sketchfab\.com/models/[^\"\\]+/textures/[^\"\\]+\.jpeg",
                page,
            )
        )
    )
    saved: list[Path] = []
    for i, url in enumerate(urls):
        dl = url if not qs else f"{url}?{qs}"
        with urllib.request.urlopen(dl, timeout=60) as resp:
            data = resp.read()
        path = TEXTURE_DIR / f"tex_{i}.jpeg"
        path.write_bytes(data)
        saved.append(path)
        print("texture", path.name, len(data))
    return saved


def pick_albedo(textures: list[Path]) -> Image.Image | None:
    if not textures:
        return None
    sized = [(p, p.stat().st_size) for p in textures]
    # Prefer mid-size JPEG (likely baseColor), not huge normal/metallic maps.
    mid = [p for p, sz in sized if 5000 <= sz <= 500_000]
    best = max(mid or [p for p, _ in sized], key=lambda p: p.stat().st_size)
    img = Image.open(best).convert("RGBA")
    if max(img.size) > 1024:
        scale = 1024 / max(img.size)
        img = img.resize((int(img.size[0] * scale), int(img.size[1] * scale)), Image.Resampling.LANCZOS)
    elif max(img.size) < 256:
        img = img.resize((512, 512), Image.Resampling.LANCZOS)
    print("albedo from", best.name, img.size)
    return img


def _sphere_point(radius: float, yaw_deg: float, pitch_deg: float) -> np.ndarray:
    yaw = math.radians(yaw_deg)
    pitch = math.radians(pitch_deg)
    x = radius * math.cos(pitch) * math.cos(yaw)
    y = radius * math.sin(pitch)
    z = radius * math.cos(pitch) * math.sin(yaw)
    return np.array([x, y, z], dtype=np.float64)


def _whip_from_surface(body_r: float, yaw_deg: float, length: float, radius: float) -> trimesh.Trimesh:
    base = _sphere_point(body_r, yaw_deg, 0.0)
    tip_dir = np.array(
        [
            math.cos(math.radians(yaw_deg)) * 0.35,
            0.15,
            math.sin(math.radians(yaw_deg)) * 0.35,
        ],
        dtype=np.float64,
    )
    tip_dir /= np.linalg.norm(tip_dir)
    tip = base + tip_dir * length
    seg = trimesh.creation.cylinder(radius=radius, height=length, sections=12)
    direction = tip - base
    direction /= np.linalg.norm(direction)
    z = np.array([0.0, 1.0, 0.0])
    rot = trimesh.geometry.align_vectors(z, direction)
    seg.apply_transform(rot)
    seg.apply_translation((base + tip) * 0.5)
    return seg


def build_luna1_scene(albedo: Image.Image | None) -> trimesh.Scene:
    silver = (185, 188, 195, 255)
    gold = (205, 165, 60, 255)
    dark = (45, 48, 55, 255)

    body_r = 0.36
    body = trimesh.creation.icosphere(subdivisions=4, radius=body_r)
    if albedo is not None:
        # Spherical UV projection for icosphere.
        v = body.vertices
        u = 0.5 + np.arctan2(v[:, 2], v[:, 0]) / (2.0 * math.pi)
        vv = 0.5 - np.arcsin(np.clip(v[:, 1] / body_r, -1.0, 1.0)) / math.pi
        body.visual = trimesh.visual.texture.TextureVisuals(
            uv=np.column_stack([u, vv]), image=albedo
        )
    else:
        body.visual.vertex_colors = np.tile(silver, (len(body.vertices), 1))

    band = trimesh.creation.cylinder(radius=body_r * 1.02, height=0.05, sections=48)
    band.visual.vertex_colors = np.tile((160, 162, 168, 255), (len(band.vertices), 1))

    adapter = trimesh.creation.cone(radius=body_r * 0.75, height=0.22, sections=32)
    adapter.apply_translation([0.0, -body_r - 0.11, 0.0])
    adapter.visual.vertex_colors = np.tile(dark, (len(adapter.vertices), 1))

    pods: list[trimesh.Trimesh] = []
    for yaw in (35.0, 145.0, 225.0, 315.0):
        p = _sphere_point(body_r * 0.98, yaw, 8.0)
        pod = trimesh.creation.box(extents=[0.08, 0.05, 0.06])
        pod.apply_translation(p)
        pod.visual.vertex_colors = np.tile((120, 125, 132, 255), (len(pod.vertices), 1))
        pods.append(pod)

    whips = [_whip_from_surface(body_r * 0.98, yaw, 0.62, 0.012) for yaw in (0.0, 90.0, 180.0, 270.0)]
    for w in whips:
        w.visual.vertex_colors = np.tile(dark, (len(w.vertices), 1))

    mast = trimesh.creation.cylinder(radius=0.018, height=0.42, sections=16)
    mast.apply_translation([0.0, body_r + 0.21, 0.0])
    mast.visual.vertex_colors = np.tile(gold, (len(mast.vertices), 1))

    tip = trimesh.creation.icosphere(subdivisions=2, radius=0.045)
    tip.apply_translation([0.0, body_r + 0.44, 0.0])
    tip.visual.vertex_colors = np.tile(gold, (len(tip.vertices), 1))

    scene = trimesh.Scene()
    scene.add_geometry(body, geom_name="Body")
    scene.add_geometry(band, geom_name="Band")
    scene.add_geometry(adapter, geom_name="Adapter")
    for i, pod in enumerate(pods):
        scene.add_geometry(pod, geom_name=f"Pod{i}")
    for i, whip in enumerate(whips):
        scene.add_geometry(whip, geom_name=f"Whip{i}")
    scene.add_geometry(mast, geom_name="Mast")
    scene.add_geometry(tip, geom_name="Tip")
    return scene


def main() -> int:
    textures = fetch_viewer_textures()
    albedo = pick_albedo(textures)
    scene = build_luna1_scene(albedo)
    scene.export(str(OUT_GLB))
    print("Wrote", OUT_GLB, "bytes", OUT_GLB.stat().st_size)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
