"""
Convert a spacecraft GLB (or procedural parts) to Unity-ready OBJ + PNG under
Assets/Resources/ProbeMeshes/<OutName>/.

Usage:
  python Tools/_probe_mesh_import/convert_glb_to_probe_obj.py --src path.glb --out-name NewHorizons
  python Tools/_probe_mesh_import/convert_glb_to_probe_obj.py --src path.glb --out-name Juno --target-faces 18000
  python Tools/_probe_mesh_import/convert_glb_to_probe_obj.py --procedural Venera7 --out-name Venera7
"""
from __future__ import annotations

import argparse
import json
import math
import shutil
import uuid
from pathlib import Path

import numpy as np
import trimesh
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT_ROOT = ROOT / "Assets" / "Resources" / "ProbeMeshes"
DEFAULT_TARGET_FACES = 18000


def new_guid() -> str:
    return uuid.uuid4().hex


def read_existing_guid(meta_path: Path) -> str | None:
    if not meta_path.is_file():
        return None
    for line in meta_path.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            return line.split("guid: ", 1)[1].strip()
    return None


def write_texture_meta(path: Path, guid: str) -> None:
    path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbook: 0
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 0
  cookieLight: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Android
    maxTextureSize: 1024
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
  spritePackingTag: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def write_model_meta(path: Path, guid: str) -> None:
    path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
ModelImporter:
  serializedVersion: 22200
  internalIDToNameTable: []
  externalObjects: {{}}
  materials:
    materialImportMode: 1
    materialName: 0
    materialSearch: 1
    materialLocation: 1
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    removeConstantScaleCurves: 0
    motionNodeName: 
    rigImportErrors: 
    rigImportWarnings: 
    animationImportErrors: 
    animationImportWarnings: 
    animationRetargetingWarnings: 
    animationDoRetargetingWarnings: 0
    importAnimatedCustomProperties: 0
    importConstraints: 0
    animationCompression: 1
    animationRotationError: 0.5
    animationPositionError: 0.5
    animationScaleError: 0.5
    animationWrapMode: 0
    extraExposedTransformPaths: []
    extraUserProperties: []
    clipAnimations: []
    isReadable: 0
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 1
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    importPhysicalCameras: 1
    importVisibility: 1
    importBlendShapes: 1
    importCameras: 0
    importLights: 0
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 1
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    meshOptimizationFlags: -1
    autoGenerateAvatarMask: 1
    normalImportMode: 0
    tangentImportMode: 0
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
  tangentSpace:
    normalSmoothAngle: 60
    normalTangentAngle: 60
  referencedClips: []
  importAnimation: 0
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 0
  humanoidOversampling: 1
  avatarSetup: 0
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  importBlendShapeDeformPercent: 1
  remapMaterialsIfMaterialImportModeIsNone: 0
  additionalBone: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def write_default_meta(path: Path, guid: str) -> None:
    path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def as_pil(image) -> Image.Image | None:
    if image is None:
        return None
    if isinstance(image, Image.Image):
        return image.convert("RGBA") if image.mode not in ("RGB", "RGBA") else image
    arr = np.asarray(image)
    if arr.ndim == 2:
        return Image.fromarray(arr.astype(np.uint8), mode="L").convert("RGBA")
    if arr.shape[-1] == 3:
        return Image.fromarray(arr.astype(np.uint8), mode="RGB")
    return Image.fromarray(arr.astype(np.uint8), mode="RGBA")


def solid_texture(rgba, size: int = 16) -> Image.Image:
    c = tuple(int(x) for x in np.asarray(rgba).flatten()[:4])
    if len(c) == 3:
        c = (*c, 255)
    return Image.new("RGBA", (size, size), c)


def transformed_mesh(scene: trimesh.Scene, geom_name: str, geom: trimesh.Trimesh) -> trimesh.Trimesh:
    mesh = geom.copy()
    for node_name in scene.graph.nodes_geometry:
        gname = scene.graph[node_name][1]
        if gname == geom_name:
            transform, _ = scene.graph[node_name]
            mesh.apply_transform(transform)
            break
    return mesh


def decimate_mesh(mesh: trimesh.Trimesh, target_faces: int) -> trimesh.Trimesh:
    if len(mesh.faces) <= target_faces:
        return mesh
    try:
        simplified = mesh.simplify_quadric_decimation(face_count=target_faces)
        if isinstance(simplified, trimesh.Trimesh) and len(simplified.faces) > 0:
            return simplified
    except Exception as exc:  # noqa: BLE001
        raise RuntimeError(
            "Mesh decimation failed. Install fast_simplification: pip install fast_simplification"
        ) from exc
    raise RuntimeError("Mesh decimation returned an empty mesh.")


def export_obj_manual(meshes: list[tuple[str, trimesh.Trimesh, str]], out_obj: Path) -> None:
    mtl_name = out_obj.with_suffix(".mtl").name
    obj_lines = [f"mtllib {mtl_name}", ""]
    mtl_lines: list[str] = []
    v_off = 0
    vt_off = 0
    vn_off = 0

    for mat_name, mesh, tex_file in meshes:
        mtl_lines.append(f"newmtl {mat_name}")
        mtl_lines.append("Kd 1.0 1.0 1.0")
        mtl_lines.append("Ka 0.0 0.0 0.0")
        mtl_lines.append("Ks 0.1 0.1 0.1")
        mtl_lines.append("d 1.0")
        mtl_lines.append(f"map_Kd {tex_file}")
        mtl_lines.append("")

        obj_lines.append(f"o {mat_name}")
        obj_lines.append(f"usemtl {mat_name}")
        verts = mesh.vertices
        norms = mesh.vertex_normals
        uvs = None
        if hasattr(mesh.visual, "uv") and mesh.visual.uv is not None:
            uvs = np.asarray(mesh.visual.uv)

        for v in verts:
            obj_lines.append(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}")
        for n in norms:
            obj_lines.append(f"vn {n[0]:.6f} {n[1]:.6f} {n[2]:.6f}")
        if uvs is not None:
            for uv in uvs:
                obj_lines.append(f"vt {uv[0]:.6f} {uv[1]:.6f}")
        else:
            for _ in verts:
                obj_lines.append("vt 0.0 0.0")
            uvs = np.zeros((len(verts), 2), dtype=np.float64)

        for face in mesh.faces:
            a, b, c = (int(i) + 1 for i in face)
            obj_lines.append(
                "f "
                f"{a + v_off}/{a + vt_off}/{a + vn_off} "
                f"{b + v_off}/{b + vt_off}/{b + vn_off} "
                f"{c + v_off}/{c + vt_off}/{c + vn_off}"
            )
        obj_lines.append("")
        v_off += len(verts)
        vt_off += len(uvs)
        vn_off += len(norms)

    out_obj.write_text("\n".join(obj_lines) + "\n", encoding="utf-8")
    out_obj.with_suffix(".mtl").write_text("\n".join(mtl_lines) + "\n", encoding="utf-8")


def package_from_meshes(
    out: Path,
    named: list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int] | None]],
    source_note: str,
) -> tuple[list[tuple[str, trimesh.Trimesh, str]], dict]:
    packaged: list[tuple[str, trimesh.Trimesh, str]] = []
    all_meshes: list[trimesh.Trimesh] = []

    for name, mesh, rgba in named:
        safe = "".join(c if c.isalnum() or c in "._-" else "_" for c in name)
        mat_name = f"mat_{safe}"
        tex_file = f"{safe}_albedo.png"

        img = None
        color = rgba or (200, 200, 200, 255)
        if hasattr(mesh.visual, "material"):
            mat = mesh.visual.material
            img = as_pil(getattr(mat, "baseColorTexture", None))
            mc = getattr(mat, "main_color", None)
            if mc is not None:
                color = [int(x) for x in np.asarray(mc).flatten()[:4]]
                if len(color) == 3:
                    color.append(255)
        if img is None and hasattr(mesh.visual, "image"):
            img = as_pil(mesh.visual.image)
        if img is None:
            img = solid_texture(color, 16)

        img.save(out / tex_file, format="PNG")
        write_texture_meta(out / f"{tex_file}.meta", new_guid())

        uv = getattr(getattr(mesh, "visual", None), "uv", None)
        if uv is not None:
            mesh.visual = trimesh.visual.texture.TextureVisuals(uv=uv, image=img)

        packaged.append((mat_name, mesh, tex_file))
        all_meshes.append(mesh)

    combined = trimesh.util.concatenate(all_meshes) if len(all_meshes) > 1 else all_meshes[0]
    bounds = combined.bounds
    extents = combined.extents
    antenna_local = [
        float(combined.centroid[0]),
        float(bounds[1][1] - extents[1] * 0.05),
        float(combined.centroid[2]),
    ]
    info = {
        "bounds_min": bounds[0].tolist(),
        "bounds_max": bounds[1].tolist(),
        "extents": extents.tolist(),
        "center": combined.centroid.tolist(),
        "antenna_local": antenna_local,
        "suggested_visual_euler_xyz": [0.0, 0.0, 0.0],
        "source": source_note,
        "faces": int(len(combined.faces)),
        "verts": int(len(combined.vertices)),
    }
    return packaged, info


def load_glb_parts(
    path: Path, target_faces: int, label: str
) -> list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int] | None]]:
    scene = trimesh.load(str(path), force="scene")
    if isinstance(scene, trimesh.Trimesh):
        mesh = scene
        if len(mesh.faces) > target_faces:
            print(f"Decimating {len(mesh.faces)} -> ~{target_faces}")
            mesh = decimate_mesh(mesh, target_faces)
        return [(label, mesh, (200, 200, 200, 255))]

    assert isinstance(scene, trimesh.Scene)
    combined = scene.to_geometry()
    if not isinstance(combined, trimesh.Trimesh):
        raise RuntimeError(f"Could not concatenate scene from {path}")

    if len(combined.faces) > target_faces:
        print(f"Decimating combined mesh {len(combined.faces)} -> ~{target_faces}")
        combined = decimate_mesh(combined, target_faces)
        rgba = (200, 200, 200, 255)
        if hasattr(combined.visual, "main_color") and combined.visual.main_color is not None:
            mc = [int(x) for x in np.asarray(combined.visual.main_color).flatten()[:4]]
            if len(mc) == 3:
                mc.append(255)
            rgba = tuple(mc)  # type: ignore[assignment]
        # Prefer textured materials from scene when possible.
        parts: list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int] | None]] = []
        for name, geom in scene.geometry.items():
            if not isinstance(geom, trimesh.Trimesh):
                continue
            mesh = transformed_mesh(scene, name, geom)
            rgba_p = None
            if hasattr(mesh.visual, "material"):
                mat = mesh.visual.material
                color = getattr(mat, "main_color", None)
                if color is not None:
                    rgba_p = [int(x) for x in np.asarray(color).flatten()[:4]]
                    if len(rgba_p) == 3:
                        rgba_p.append(255)
                    rgba_p = tuple(rgba_p)  # type: ignore[assignment]
            parts.append((name, mesh, rgba_p))
        total = sum(len(m.faces) for _, m, _ in parts)
        if total > target_faces * 1.4 or total == 0:
            return [(label, combined, rgba)]
        # Scale each part's budget.
        scale = target_faces / max(total, 1)
        out_parts = []
        for name, mesh, rgba_p in parts:
            budget = max(64, int(len(mesh.faces) * scale))
            if len(mesh.faces) > budget:
                mesh = decimate_mesh(mesh, budget)
            out_parts.append((name, mesh, rgba_p))
        return out_parts

    parts = []
    for name, geom in scene.geometry.items():
        if not isinstance(geom, trimesh.Trimesh):
            continue
        mesh = transformed_mesh(scene, name, geom)
        rgba = None
        if hasattr(mesh.visual, "material"):
            mat = mesh.visual.material
            color = getattr(mat, "main_color", None)
            if color is not None:
                rgba = [int(x) for x in np.asarray(color).flatten()[:4]]
                if len(rgba) == 3:
                    rgba.append(255)
                rgba = tuple(rgba)  # type: ignore[assignment]
        parts.append((name, mesh, rgba))
    if not parts:
        return [(label, combined, (200, 200, 200, 255))]
    return parts


def _box(name, extents, pos, rgba, yaw=0.0):
    m = trimesh.creation.box(extents=extents)
    if yaw:
        m.apply_transform(trimesh.transformations.rotation_matrix(math.radians(yaw), [0, 1, 0]))
    m.apply_translation(pos)
    return name, m, rgba


def _cyl(name, radius, height, pos, rgba, sections=24):
    m = trimesh.creation.cylinder(radius=radius, height=height, sections=sections)
    m.apply_translation(pos)
    return name, m, rgba


def _sphere(name, radius, pos, rgba, subdiv=2):
    m = trimesh.creation.icosphere(subdivisions=subdiv, radius=radius)
    m.apply_translation(pos)
    return name, m, rgba


def build_procedural(kind: str) -> tuple[list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int]]], str]:
    """Enriched procedural approximations when Sketchfab GLB is unavailable."""
    gold = (210, 165, 55, 255)
    bus = (190, 185, 170, 255)
    dark = (55, 55, 60, 255)
    dish = (230, 232, 238, 255)
    k = kind.lower()

    if k == "venera7":
        parts = [
            _sphere("Capsule", 0.42, [0, 0.08, 0], bus, 3),
            _cyl("HgaDish", 0.35, 0.04, [0, 0.55, 0], dish),
            _cyl("Leg0", 0.04, 0.28, [0.28, -0.25, 0], dark),
            _cyl("Leg1", 0.04, 0.28, [-0.14, -0.25, 0.24], dark),
            _cyl("Leg2", 0.04, 0.28, [-0.14, -0.25, -0.24], dark),
            _cyl("Base", 0.22, 0.12, [0, -0.35, 0], gold),
            _box("Instrument", [0.12, 0.1, 0.12], [0.2, 0.15, 0.15], dark),
        ]
        return parts, "Procedural Venera 7 approximation (Sketchfab GLB unavailable)"

    if k == "luna1":
        parts = [
            _cyl("Body", 0.28, 0.85, [0, 0, 0], bus),
            _cyl("Antenna", 0.04, 0.9, [0, 0.7, 0], gold),
            _sphere("Tip", 0.08, [0, 1.15, 0], gold, 2),
        ]
        for i in range(4):
            yaw = i * 90.0
            rad = math.radians(yaw)
            x, z = 0.45 * math.cos(rad), 0.45 * math.sin(rad)
            parts.append(_cyl(f"Whip{i}", 0.025, 0.55, [x, -0.05, z], dark))
        parts.append(_box("Base", [0.28, 0.1, 0.28], [0, -0.45, 0], dark))
        return parts, "Procedural Luna 1 approximation (Sketchfab GLB unavailable)"

    if k == "hayabusa2":
        parts = [
            _box("Bus", [0.7, 0.45, 0.7], [0, 0, 0], bus),
            _box("PanelL", [0.05, 0.35, 1.2], [-0.55, 0.05, 0], dark),
            _box("PanelR", [0.05, 0.35, 1.2], [0.55, 0.05, 0], dark),
            _cyl("Horn", 0.08, 0.35, [0, 0.35, -0.2], gold),
            _box("Sampler", [0.18, 0.25, 0.18], [0, -0.35, 0.15], dark),
            _box("IonEngine", [0.2, 0.12, 0.2], [0, -0.1, 0.4], gold),
        ]
        return parts, "Procedural Hayabusa2 approximation (Sketchfab GLB unavailable)"

    if k in ("change4", "chang4", "change_4"):
        parts = [
            _box("Deck", [0.95, 0.18, 0.7], [0, 0, 0], bus),
            _box("PanelL", [0.45, 0.03, 0.55], [-0.75, 0.08, 0], gold),
            _box("PanelR", [0.45, 0.03, 0.55], [0.75, 0.08, 0], gold),
            _cyl("Mast", 0.05, 0.35, [0, 0.28, -0.2], dark),
            _box("Camera", [0.14, 0.12, 0.14], [0.25, 0.18, 0.2], dark),
            _box("Payload", [0.2, 0.12, 0.16], [-0.2, -0.08, 0.28], gold),
        ]
        return parts, "Procedural Chang'e 4 approximation (Sketchfab GLB unavailable)"

    if k == "luna16":
        parts = [
            _cyl("Bus", 0.48, 0.7, [0, 0, 0], bus),
            _cyl("Ascent", 0.22, 0.45, [0, 0.5, 0], gold),
            _cyl("Antenna", 0.05, 0.7, [0.42, 0.15, 0], gold),
            _box("Drill", [0.18, 0.22, 0.14], [0, 0.28, 0.32], dark),
            _box("TankL", [0.16, 0.28, 0.16], [-0.32, -0.05, 0.18], gold),
            _box("TankR", [0.16, 0.28, 0.16], [0.32, -0.05, -0.12], bus),
        ]
        return parts, "Procedural Luna 16 approximation (Sketchfab GLB unavailable)"

    if k in ("tianwen1", "tianwen"):
        parts = [
            _box("Bus", [0.65, 0.4, 0.65], [0, 0, 0], bus),
            _cyl("HgaDish", 0.55, 0.04, [0, 0.08, -0.62], dish),
            _cyl("Rtg", 0.08, 0.35, [0.48, 0, 0.12], dark),
            _box("Solar", [0.35, 0.03, 0.28], [-0.45, 0.22, 0.1], gold),
            _sphere("Entry", 0.22, [0, -0.32, 0.25], dark, 2),
            _box("Instrument", [0.14, 0.12, 0.14], [0.35, 0.12, 0.25], gold),
        ]
        return parts, "Procedural Tianwen-1 approximation (Sketchfab GLB unavailable)"

    if k in ("change5", "chang5", "change_5"):
        parts = [
            _cyl("Lander", 0.35, 0.85, [0, 0, 0], bus),
            _box("PanelL", [0.5, 0.03, 0.38], [-0.65, 0.2, 0], gold),
            _box("PanelR", [0.5, 0.03, 0.38], [0.65, 0.2, 0], gold),
            _cyl("Ascent", 0.18, 0.4, [0, 0.55, 0], dark),
            _cyl("Mast", 0.05, 0.4, [0, 0.55, 0.18], dark),
            _box("Drill", [0.16, 0.2, 0.14], [0, -0.25, 0.22], gold),
            _cyl("Engine", 0.1, 0.2, [0.25, 0.35, 0], gold),
        ]
        return parts, "Procedural Chang'e 5 approximation (Sketchfab GLB unavailable)"

    if k == "akatsuki":
        parts = [
            _box("Bus", [0.7, 0.55, 0.7], [0, 0, 0], bus),
            _box("PanelL", [0.35, 0.03, 0.5], [-0.55, 0.1, 0], gold),
            _box("PanelR", [0.35, 0.03, 0.5], [0.55, 0.1, 0], gold),
            _cyl("HgaBoom", 0.035, 1.1, [0.7, 0.2, 0], dish),
            _cyl("Prop", 0.22, 0.18, [0, -0.28, 0], bus),
            _box("Camera", [0.12, 0.12, 0.12], [0.2, 0.28, 0.28], dark),
        ]
        return parts, "Procedural Akatsuki approximation (Sketchfab GLB unavailable)"

    if k in ("chandrayaan3", "chandrayaan"):
        parts = [
            _box("Deck", [0.7, 0.28, 0.7], [0, 0, 0], bus),
        ]
        for i in range(3):
            yaw = i * 120.0
            rad = math.radians(yaw)
            x, z = 0.65 * math.sin(rad), 0.65 * math.cos(rad)
            parts.append(_box(f"Panel{i}", [0.38, 0.03, 0.45], [x, 0.15, z], gold, yaw=yaw))
        parts.extend(
            [
                _cyl("Mast", 0.06, 0.35, [0, 0.35, 0], dark),
                _box("Camera", [0.16, 0.14, 0.14], [0.35, 0.18, 0.25], dark),
                _cyl("Thruster", 0.1, 0.14, [-0.25, -0.15, 0.2], gold),
                _box("Belly", [0.28, 0.12, 0.22], [0, -0.2, -0.15], bus),
            ]
        )
        return parts, "Procedural Chandrayaan-3 approximation (Sketchfab GLB unavailable)"

    raise SystemExit(f"Unknown procedural kind: {kind}")


ATTRIBUTIONS = {
    "NewHorizons": """New Horizons spacecraft 3D model
=================================

Source: NASA Visualization Technology Applications and Development (VTAD)
  https://science.nasa.gov/resource/new-horizons-3d-model/
  GLB: https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/n/New_Horizons.glb

NASA media generally may be used for educational / informational purposes per
NASA Images and Media Usage Guidelines (no NASA emblem endorsement implied).

Converted/decimated to OBJ + PNG for Unity mobile (no runtime glTF dependency).
""",
    "Juno": """Juno spacecraft 3D model
========================

Source: NASA Visualization Technology Applications and Development (VTAD)
  https://science.nasa.gov/resource/juno-3d-model/
  GLB: https://assets.science.nasa.gov/content/dam/science/psd/solar/2023/09/j/Juno.glb

NASA media generally may be used for educational / informational purposes per
NASA Images and Media Usage Guidelines (no NASA emblem endorsement implied).

Converted/decimated to OBJ + PNG for Unity mobile (no runtime glTF dependency).
""",
    "Venera7": """Venera 7 spacecraft 3D model
============================

Intended source (CC BY when available): Sketchfab Venera 7
  https://sketchfab.com/3d-models/venera-7-fd464eedd93a41c98d2608faeb871cd0

If this folder contains a procedural approximation, place Venera7-source.glb next to
Tools/_probe_mesh_import/ and re-run convert_glb_to_probe_obj.py, then Build Probe Mesh Prefabs.
""",
    "Luna1": """Luna 1 spacecraft 3D model
==========================

Intended source (CC BY when available): Sketchfab Soviet Moon-1 / Luna
  https://sketchfab.com/3d-models/soviet-space-satellite-moon-1-1b7feef6a0614837b21790e219e68ab2

If this folder contains a procedural approximation, place Luna1-source.glb under
Tools/_probe_mesh_import/ and re-run convert_glb_to_probe_obj.py, then Build Probe Mesh Prefabs.
""",
    "Hayabusa2": """Hayabusa2 spacecraft 3D model
=============================

Intended source: CC-licensed Sketchfab / JAXA-derived downloadable mesh when available.
Place Hayabusa2-source.glb under Tools/_probe_mesh_import/ and re-run conversion if replacing
the procedural approximation.
""",
    "Change4": """Chang'e 4 spacecraft 3D model
=============================

Intended source: CC-licensed Sketchfab downloadable mesh when available.
Place Change4-source.glb under Tools/_probe_mesh_import/ and re-run conversion if replacing
the procedural approximation.
""",
    "Luna16": """Luna 16 spacecraft 3D model
===========================

Intended source: CC-licensed Sketchfab downloadable mesh when available.
Place Luna16-source.glb under Tools/_probe_mesh_import/ and re-run conversion if replacing
the procedural approximation.
""",
    "Tianwen1": """Tianwen-1 spacecraft 3D model
=============================

Intended source: CC-licensed Sketchfab downloadable mesh when available.
Place Tianwen1-source.glb under Tools/_probe_mesh_import/ and re-run conversion if replacing
the procedural approximation.
""",
    "Change5": """Chang'e 5 spacecraft 3D model
=============================

Intended source: CC-licensed Sketchfab downloadable mesh when available.
Place Change5-source.glb under Tools/_probe_mesh_import/ and re-run conversion if replacing
the procedural approximation.
""",
    "Akatsuki": """Akatsuki (Venus Climate Orbiter) 3D model
========================================

Intended source: CC-licensed Sketchfab / JAXA-derived downloadable mesh when available.
Place Akatsuki-source.glb under Tools/_probe_mesh_import/ and re-run conversion if replacing
the procedural approximation.
""",
    "Chandrayaan3": """Chandrayaan-3 spacecraft 3D model
=================================

Intended source: CC-licensed Sketchfab downloadable mesh when available
(e.g. Vikram/Pragyan CC BY community models).
Place Chandrayaan3-source.glb under Tools/_probe_mesh_import/ and re-run conversion if replacing
the procedural approximation.
""",
}


def clear_mesh_outputs(out: Path, out_name: str) -> None:
    out.mkdir(parents=True, exist_ok=True)
    keep = {
        "Materials",
        f"{out_name}Prefab.prefab",
        f"{out_name}Prefab.prefab.meta",
        f"{out_name}.obj.meta",
    }
    for p in list(out.iterdir()):
        if p.name in keep:
            continue
        if p.is_file():
            p.unlink()
        elif p.is_dir() and p.name != "Materials":
            shutil.rmtree(p)


def convert(
    *,
    src: Path | None,
    out_name: str,
    target_faces: int,
    procedural: str | None,
    attribution_key: str | None,
    source_note: str | None,
) -> Path:
    out = OUT_ROOT / out_name
    clear_mesh_outputs(out, out_name)

    if src is not None and src.exists():
        print("Using GLB", src)
        named = load_glb_parts(src, target_faces, out_name)
        note = source_note or f"Imported GLB {src.name}"
        used_glb = True
    elif procedural:
        print("Building procedural", procedural)
        named, note = build_procedural(procedural)
        used_glb = False
    else:
        raise SystemExit("Provide --src GLB or --procedural kind")

    packaged, info = package_from_meshes(out, named, note)
    (out / "mesh_info.json").write_text(json.dumps(info, indent=2), encoding="utf-8")
    write_default_meta(out / "mesh_info.json.meta", new_guid())

    obj_path = out / f"{out_name}.obj"
    export_obj_manual(packaged, obj_path)
    obj_meta = out / f"{out_name}.obj.meta"
    write_model_meta(obj_meta, read_existing_guid(obj_meta) or new_guid())
    write_default_meta(out / f"{out_name}.mtl.meta", new_guid())

    attr_key = attribution_key or out_name
    attr = ATTRIBUTIONS.get(attr_key, f"{out_name}\nSource: {note}\n")
    if used_glb and attr_key in ("Venera7", "Luna1", "Hayabusa2", "Change4"):
        attr = (
            f"{out_name} spacecraft 3D model\n"
            f"{'=' * (len(out_name) + 22)}\n\n"
            f"Imported from local GLB: {src}\n"
            f"Converted/decimated to OBJ + PNG for Unity mobile.\n"
            f"Verify and keep creator attribution (CC BY) as required.\n"
        )
    (out / "ATTRIBUTION.txt").write_text(attr, encoding="utf-8")
    write_default_meta(out / "ATTRIBUTION.txt.meta", new_guid())
    write_default_meta(Path(str(out) + ".meta"), new_guid())

    import_dir = Path(__file__).resolve().parent
    (import_dir / f"{out_name}_antenna_local.json").write_text(
        json.dumps(
            {
                "antenna_local": info["antenna_local"],
                "visual_euler": info["suggested_visual_euler_xyz"],
                "faces": info["faces"],
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Exported to", out)
    print("OBJ size", obj_path.stat().st_size, "faces", info["faces"])
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--src", type=Path, help="Source .glb")
    parser.add_argument("--out-name", required=True, help="Folder/name under ProbeMeshes")
    parser.add_argument("--target-faces", type=int, default=DEFAULT_TARGET_FACES)
    parser.add_argument(
        "--procedural",
        help="Procedural kind if no GLB: Venera7|Luna1|Hayabusa2|Change4|Luna16|Tianwen1|Change5|Akatsuki|Chandrayaan3",
    )
    parser.add_argument("--attribution-key", help="Key in ATTRIBUTIONS dict")
    parser.add_argument("--source-note", help="mesh_info source string")
    args = parser.parse_args()
    convert(
        src=args.src,
        out_name=args.out_name,
        target_faces=args.target_faces,
        procedural=args.procedural,
        attribution_key=args.attribution_key,
        source_note=args.source_note,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
