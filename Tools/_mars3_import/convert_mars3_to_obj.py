"""Convert Mars 3 GLB (if present) or build procedural game-ready OBJ + PNG.

Preferred source: Wieslaw Kruczala Mars 3 spacecraft on Sketchfab (CC BY 4.0)
  https://sketchfab.com/3d-models/mars-3-spacecraft-5b7853d53cd84b6ca6c16fe68a92c98a
Sketchfab Download API requires an authenticated token; place the GLB next to this
script as Mars3-source.glb when available. Otherwise a procedural approximation is
exported (cylinder bus, 4 gold petals, spherical lander, HGA dish).
"""
from __future__ import annotations

import json
import shutil
import uuid
from pathlib import Path

import numpy as np
import trimesh
from PIL import Image

ROOT = Path(__file__).resolve().parent
SRC_GLB = ROOT / "Mars3-source.glb"
OUT = (
    Path(__file__).resolve().parents[2]
    / "Assets"
    / "Resources"
    / "ProbeMeshes"
    / "Mars3"
)

# Target triangle budget for mobile (Voyager-scale).
TARGET_FACES = 18000


def new_guid() -> str:
    return uuid.uuid4().hex


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
        print("decimate failed:", exc)
    # Fallback: random face subsample (keeps UV poorly but reduces size).
    ratio = target_faces / max(len(mesh.faces), 1)
    idx = np.linspace(0, len(mesh.faces) - 1, num=target_faces, dtype=int)
    return mesh.submesh([idx], append=True)


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


def colored_primitive(mesh: trimesh.Trimesh, rgba) -> trimesh.Trimesh:
    mesh = mesh.copy()
    color = np.array(rgba, dtype=np.uint8)
    if color.shape[0] == 3:
        color = np.append(color, 255)
    mesh.visual = trimesh.visual.ColorVisuals(mesh=mesh, face_colors=color)
    return mesh


def build_procedural_parts() -> list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int]]]:
    """Mars 3-like orbiter: cylindrical bus, 4 gold petals, lander sphere, HGA."""
    parts: list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int]]] = []

    bus = trimesh.creation.cylinder(radius=0.55, height=0.55, sections=32)
    bus.apply_translation([0.0, 0.0, 0.0])
    parts.append(("Bus", bus, (190, 185, 170, 255)))

    ring = trimesh.creation.cylinder(radius=0.72, height=0.06, sections=32)
    ring.apply_translation([0.0, 0.12, 0.0])
    parts.append(("BusRing", ring, (160, 155, 145, 255)))

    for i in range(4):
        yaw = np.deg2rad(45.0 + i * 90.0)
        petal = trimesh.creation.box(extents=[0.55, 0.03, 0.72])
        # Shift outward along local +Z then rotate around Y.
        petal.apply_translation([0.0, 0.22, 0.78])
        rot = trimesh.transformations.rotation_matrix(yaw, [0, 1, 0])
        petal.apply_transform(rot)
        # Slight outward tilt around axis perpendicular to petal radial direction.
        axis = np.array([np.cos(yaw), 0.0, -np.sin(yaw)])
        tilt = trimesh.transformations.rotation_matrix(np.deg2rad(18.0), axis)
        petal.apply_transform(tilt)
        parts.append((f"Petal{i}", petal, (210, 165, 55, 255)))

    lander = trimesh.creation.icosphere(subdivisions=3, radius=0.38)
    lander.apply_translation([0.0, -0.55, 0.0])
    parts.append(("Lander", lander, (55, 55, 60, 255)))

    mast = trimesh.creation.cylinder(radius=0.045, height=0.7, sections=16)
    mast.apply_translation([0.0, 0.55, 0.0])
    parts.append(("HgaMast", mast, (120, 120, 125, 255)))

    dish = trimesh.creation.cylinder(radius=0.42, height=0.05, sections=28)
    dish.apply_translation([0.0, 0.92, 0.0])
    parts.append(("HgaDish", dish, (230, 232, 238, 255)))

    # Small instrument boxes for silhouette.
    for i, offset in enumerate(([0.35, 0.05, 0.2], [-0.32, 0.08, -0.18])):
        box = trimesh.creation.box(extents=[0.18, 0.14, 0.16])
        box.apply_translation(offset)
        parts.append((f"Instrument{i}", box, (70, 72, 78, 255)))

    return parts


def package_from_meshes(
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
        if img is None:
            img = solid_texture(color, 16)

        img.save(OUT / tex_file, format="PNG")
        write_texture_meta(OUT / f"{tex_file}.meta", new_guid())

        uv = getattr(getattr(mesh, "visual", None), "uv", None)
        if uv is not None:
            mesh.visual = trimesh.visual.texture.TextureVisuals(uv=uv, image=img)

        packaged.append((mat_name, mesh, tex_file))
        all_meshes.append(mesh)

    combined = trimesh.util.concatenate(all_meshes) if len(all_meshes) > 1 else all_meshes[0]
    bounds = combined.bounds
    extents = combined.extents
    # Antenna near HGA dish top (Y-up).
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


def load_glb_parts(path: Path) -> list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int] | None]]:
    scene = trimesh.load(str(path), force="scene")
    assert isinstance(scene, trimesh.Scene)
    combined = scene.to_geometry()
    if len(combined.faces) > TARGET_FACES:
        print(f"Decimating combined mesh {len(combined.faces)} -> ~{TARGET_FACES}")
        combined = decimate_mesh(combined, TARGET_FACES)
        # After global decimate, export as single material for stability.
        rgba = (200, 200, 200, 255)
        if hasattr(combined.visual, "main_color"):
            mc = combined.visual.main_color
            if mc is not None:
                rgba = tuple(int(x) for x in np.asarray(mc).flatten()[:4])  # type: ignore[assignment]
                if len(rgba) == 3:
                    rgba = (*rgba, 255)  # type: ignore[assignment]
        return [("Mars3", combined, rgba)]

    parts: list[tuple[str, trimesh.Trimesh, tuple[int, int, int, int] | None]] = []
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
    return parts


def write_attribution(used_glb: bool) -> None:
    if used_glb:
        text = """Mars 3 spacecraft 3D model
=============================

Source model: Mars 3 spacecraft by Wieslaw Kruczala
  https://sketchfab.com/3d-models/mars-3-spacecraft-5b7853d53cd84b6ca6c16fe68a92c98a
  Author: Wieslaw Kruczala (@Wieslaw_Kruczala)
  License: Creative Commons Attribution 4.0 (CC BY 4.0)
  https://creativecommons.org/licenses/by/4.0/

Converted/decimated to OBJ + PNG for Unity mobile (no runtime glTF dependency).
Commercial use allowed with attribution.
"""
    else:
        text = """Mars 3 spacecraft 3D model
=============================

Intended source (CC BY 4.0, commercial OK): Mars 3 spacecraft by Wieslaw Kruczala
  https://sketchfab.com/3d-models/mars-3-spacecraft-5b7853d53cd84b6ca6c16fe68a92c98a
  Download requires Sketchfab authentication; GLB was not available at import time.

This folder contains an original procedural approximation (cylinder bus, four gold
petals, spherical lander, HGA dish) exported to OBJ + PNG for Unity.
To replace with the Sketchfab mesh: save Mars3-source.glb next to
Tools/_mars3_import/convert_mars3_to_obj.py and re-run that script, then rebuild
the prefab via Solar System → Build Mars 3 Prefab.
"""
    (OUT / "ATTRIBUTION.txt").write_text(text, encoding="utf-8")
    write_default_meta(OUT / "ATTRIBUTION.txt.meta", new_guid())


def main() -> None:
    if OUT.exists():
        # Keep Materials/prefab if present — wipe mesh outputs only when rebuilding from script.
        for p in list(OUT.iterdir()):
            if p.name in ("Materials", "Mars3Prefab.prefab", "Mars3Prefab.prefab.meta"):
                continue
            if p.is_file():
                p.unlink()
            elif p.is_dir() and p.name != "Materials":
                shutil.rmtree(p)
    OUT.mkdir(parents=True, exist_ok=True)

    used_glb = SRC_GLB.exists()
    if used_glb:
        print("Using GLB", SRC_GLB)
        named = load_glb_parts(SRC_GLB)
        source_note = "Sketchfab CC BY Wieslaw Kruczala Mars 3 (decimated)"
    else:
        print("GLB missing; building procedural Mars 3 approximation")
        named = [(n, m, c) for n, m, c in build_procedural_parts()]
        source_note = "Procedural Mars 3 approximation (Sketchfab GLB unavailable)"

    packaged, info = package_from_meshes(named, source_note)
    (OUT / "mesh_info.json").write_text(json.dumps(info, indent=2), encoding="utf-8")
    write_default_meta(OUT / "mesh_info.json.meta", new_guid())

    obj_path = OUT / "Mars3.obj"
    export_obj_manual(packaged, obj_path)
    write_model_meta(OUT / "Mars3.obj.meta", new_guid())
    write_default_meta(OUT / "Mars3.mtl.meta", new_guid())
    write_attribution(used_glb)

    pm_meta = Path(str(OUT.parent) + ".meta")
    if not pm_meta.exists():
        write_default_meta(pm_meta, new_guid())
    write_default_meta(Path(str(OUT) + ".meta"), new_guid())

    print("Exported to", OUT)
    print("OBJ size", obj_path.stat().st_size)
    print("files:", sorted(p.name for p in OUT.iterdir() if not p.name.endswith(".meta")))
    print("mesh_info:", json.dumps(info, indent=2))
    # Write antenna hint for C# constants.
    (ROOT / "antenna_local.json").write_text(
        json.dumps({"antenna_local": info["antenna_local"], "visual_euler": info["suggested_visual_euler_xyz"]}, indent=2),
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
