"""Convert NASA Voyager VTAD GLB to Unity-friendly OBJ + PNG textures."""
from __future__ import annotations

import json
import shutil
import uuid
from pathlib import Path

import numpy as np
import trimesh
from PIL import Image

SRC = Path(__file__).resolve().parent / "Voyager-VTAD.glb"
OUT = (
    Path(__file__).resolve().parents[2]
    / "Assets"
    / "Resources"
    / "ProbeMeshes"
    / "Voyager1"
)


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


def solid_texture(rgba, size: int = 8) -> Image.Image:
    c = tuple(int(x) for x in np.asarray(rgba).flatten()[:4])
    if len(c) == 3:
        c = (*c, 255)
    img = Image.new("RGBA", (size, size), c)
    return img


def transformed_mesh(scene: trimesh.Scene, geom_name: str, geom: trimesh.Trimesh) -> trimesh.Trimesh:
    mesh = geom.copy()
    for node_name in scene.graph.nodes_geometry:
        gname = scene.graph[node_name][1]
        if gname == geom_name:
            transform, _ = scene.graph[node_name]
            mesh.apply_transform(transform)
            break
    return mesh


def export_obj_manual(meshes: list[tuple[str, trimesh.Trimesh, str]], out_obj: Path) -> None:
    """Write a multi-object OBJ + MTL with map_Kd PNG references."""
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
            # Generate dummy UVs so faces stay consistent.
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


def main() -> None:
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True, exist_ok=True)

    scene = trimesh.load(str(SRC), force="scene")
    assert isinstance(scene, trimesh.Scene)

    combined = scene.to_geometry()
    bounds = combined.bounds
    extents = combined.extents
    dish_hint = {
        "bounds_min": bounds[0].tolist(),
        "bounds_max": bounds[1].tolist(),
        "extents": extents.tolist(),
        "center": combined.centroid.tolist(),
        "antenna_local": [
            float(combined.centroid[0]),
            float(bounds[1][1] - extents[1] * 0.08),
            float(combined.centroid[2]),
        ],
        "suggested_visual_euler_xyz": [90.0, 0.0, 0.0],
        "source": "NASA VTAD Voyager.glb (fallback; Probe B uses unsupported Draco)",
        "faces": int(len(combined.faces)),
        "verts": int(len(combined.vertices)),
    }
    (OUT / "mesh_info.json").write_text(json.dumps(dish_hint, indent=2), encoding="utf-8")
    write_default_meta(OUT / "mesh_info.json.meta", new_guid())

    packaged: list[tuple[str, trimesh.Trimesh, str]] = []
    for name, geom in scene.geometry.items():
        if not isinstance(geom, trimesh.Trimesh):
            continue
        mesh = transformed_mesh(scene, name, geom)
        safe = "".join(c if c.isalnum() or c in "._-" else "_" for c in name)
        mat_name = f"mat_{safe}"
        tex_file = f"{safe}_albedo.png"

        img = None
        rgba = [200, 200, 200, 255]
        if hasattr(mesh.visual, "material"):
            mat = mesh.visual.material
            img = as_pil(getattr(mat, "baseColorTexture", None))
            color = getattr(mat, "main_color", None)
            if color is not None:
                rgba = [int(x) for x in np.asarray(color).flatten()[:4]]
                if len(rgba) == 3:
                    rgba.append(255)
        if img is None:
            img = solid_texture(rgba, 16)

        img.save(OUT / tex_file, format="PNG")
        write_texture_meta(OUT / f"{tex_file}.meta", new_guid())

        # Keep UV if present.
        uv = getattr(getattr(mesh, "visual", None), "uv", None)
        if uv is not None:
            mesh.visual = trimesh.visual.texture.TextureVisuals(uv=uv, image=img)
        packaged.append((mat_name, mesh, tex_file))

    obj_path = OUT / "Voyager1.obj"
    export_obj_manual(packaged, obj_path)
    write_model_meta(OUT / "Voyager1.obj.meta", new_guid())
    write_default_meta(OUT / "Voyager1.mtl.meta", new_guid())

    attribution = """Voyager 1 spacecraft 3D model
================================

Source model: NASA Visualization Technology Applications and Development (VTAD)
  https://science.nasa.gov/resource/voyager-3d-model/
  File: Voyager.glb (~3 MB)

Primary candidate (not used in-repo): NASA Science 3D Resources — Voyager Probe (B)
  https://science.nasa.gov/3d-resources/voyager-probe-b/
  Voyager Probe (B).glb uses KHR_draco_mesh_compression; conversion tools here could not
  decode mesh positions, so VTAD glTF was used as specified fallback.

License: U.S. government work / public domain (NASA).
Converted to OBJ + PNG for Unity (no runtime glTF / glTFast dependency).
"""
    (OUT / "ATTRIBUTION.txt").write_text(attribution, encoding="utf-8")
    write_default_meta(OUT / "ATTRIBUTION.txt.meta", new_guid())

    pm_meta = Path(str(OUT.parent) + ".meta")
    if not pm_meta.exists():
        write_default_meta(pm_meta, new_guid())
    v1_meta = Path(str(OUT) + ".meta")
    write_default_meta(v1_meta, new_guid())

    print("Exported to", OUT)
    print("OBJ size", obj_path.stat().st_size)
    print("files:", sorted(p.name for p in OUT.iterdir() if not p.name.endswith(".meta")))
    print("mesh_info:", json.dumps(dish_hint, indent=2))


if __name__ == "__main__":
    main()
