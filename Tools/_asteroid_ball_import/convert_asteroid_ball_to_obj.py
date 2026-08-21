"""Convert Free Asteroid Ball GLB → Unity OBJ + PNG textures (3 sun-distance variants).

Source: Lucas Patez / lukpatez — Free Asteroid Ball on Sketchfab
  https://sketchfab.com/3d-models/free-asteroid-ball-7cd9225dd6f841638f6fa747fecbe260
License: Sketchfab Free Standard (attribution required; NoAI).

The GLB packs three material variants on identical geometry:
  Asteroid_1 — cold rock (no emissive)
  Asteroid_2 — warm / lava cracks (emissive)
  Asteroid_3 — hot / strong emissive
"""
from __future__ import annotations

import json
import struct
import uuid
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent
SRC_GLB = ROOT / "free_asteroid_ball.glb"
OUT = (
    Path(__file__).resolve().parents[2]
    / "Assets"
    / "Resources"
    / "AsteroidMeshes"
    / "AsteroidBall"
)

# Match Unity sphere radius ~0.5 after import.
TARGET_RADIUS = 0.5


def new_guid() -> str:
    return uuid.uuid4().hex


def write_texture_meta(path: Path, guid: str, *, srgb: bool = True, normal: bool = False) -> None:
    texture_type = 1 if normal else 0  # NormalMap vs Default
    linear = 0 if srgb else 1
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
    sRGBTexture: {1 if srgb else 0}
    linearTexture: {linear}
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
  maxTextureSize: 1024
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
  textureType: {texture_type}
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
    maxTextureSize: 1024
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
    maxTextureSize: 512
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
    materialImportMode: 0
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
    isReadable: 1
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


def write_folder_meta(path: Path, guid: str) -> None:
    path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def load_glb(path: Path):
    with open(path, "rb") as f:
        magic = f.read(4)
        if magic != b"glTF":
            raise RuntimeError(f"Not a GLB: {path}")
        _version = struct.unpack("<I", f.read(4))[0]
        length = struct.unpack("<I", f.read(4))[0]
        json_data = None
        blob = None
        while f.tell() < length:
            chunk_len = struct.unpack("<I", f.read(4))[0]
            chunk_type = f.read(4)
            data = f.read(chunk_len)
            if chunk_type == b"JSON":
                json_data = json.loads(data.decode("utf-8"))
            elif chunk_type == b"BIN\x00":
                blob = data
    if json_data is None or blob is None:
        raise RuntimeError("GLB missing JSON or BIN chunk")
    return json_data, blob


def read_accessor(gltf, blob: bytes, accessor_index: int) -> np.ndarray:
    acc = gltf["accessors"][accessor_index]
    bv = gltf["bufferViews"][acc["bufferView"]]
    offset = bv.get("byteOffset", 0) + acc.get("byteOffset", 0)
    count = acc["count"]
    ctype = acc["componentType"]
    typ = acc["type"]
    comps = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[typ]
    dtype = {5121: np.uint8, 5123: np.uint16, 5125: np.uint32, 5126: np.float32}[ctype]
    stride = bv.get("byteStride", 0)
    item_size = np.dtype(dtype).itemsize * comps
    if stride and stride != item_size:
        raw = np.frombuffer(blob, dtype=np.uint8, count=count * stride, offset=offset)
        raw = raw.reshape(count, stride)[:, :item_size]
        arr = np.frombuffer(raw.tobytes(), dtype=dtype).reshape(count, comps)
    else:
        arr = np.frombuffer(blob, dtype=dtype, count=count * comps, offset=offset)
        arr = arr.reshape(count, comps) if comps > 1 else arr
    return np.asarray(arr, dtype=np.float32 if ctype == 5126 else arr.dtype)


def extract_images(gltf, blob: bytes, out_dir: Path) -> list[Path]:
    paths = []
    for i, img in enumerate(gltf["images"]):
        bv = gltf["bufferViews"][img["bufferView"]]
        start = bv.get("byteOffset", 0)
        data = blob[start : start + bv["byteLength"]]
        mime = img.get("mimeType", "image/png")
        ext = "png" if "png" in mime else "jpg"
        p = out_dir / f"_raw_image_{i}.{ext}"
        p.write_bytes(data)
        paths.append(p)
    return paths


def save_rgb(im: Image.Image, path: Path, size: int = 1024) -> None:
    rgb = im.convert("RGB")
    if max(rgb.size) > size:
        rgb = rgb.resize((size, size), Image.Resampling.LANCZOS)
    rgb.save(path, optimize=True)


def export_obj(
    positions: np.ndarray,
    normals: np.ndarray,
    uvs: np.ndarray,
    indices: np.ndarray,
    path: Path,
) -> dict:
    # Center + normalize to TARGET_RADIUS
    center = (positions.min(axis=0) + positions.max(axis=0)) * 0.5
    pos = positions - center
    radius = float(np.linalg.norm(pos, axis=1).max())
    scale = TARGET_RADIUS / max(radius, 1e-6)
    pos = pos * scale

    # Sketchfab Y-up with Z-forward flip in node matrix; keep Y-up for Unity.
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("# Free Asteroid Ball — shared geometry for 3 sun-distance variants\n")
        f.write("o AsteroidBall\n")
        for v in pos:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        for n in normals:
            nn = n / max(np.linalg.norm(n), 1e-8)
            f.write(f"vn {nn[0]:.6f} {nn[1]:.6f} {nn[2]:.6f}\n")
        for uv in uvs:
            f.write(f"vt {uv[0]:.6f} {1.0 - uv[1]:.6f}\n")
        f.write("s 1\n")
        for i in range(0, len(indices), 3):
            a, b, c = int(indices[i]) + 1, int(indices[i + 1]) + 1, int(indices[i + 2]) + 1
            f.write(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}\n")

    return {
        "vertexCount": int(len(pos)),
        "triangleCount": int(len(indices) // 3),
        "radius": TARGET_RADIUS,
        "sourceScale": scale,
        "center": center.tolist(),
    }


def main() -> None:
    if not SRC_GLB.exists():
        raise SystemExit(f"Missing source GLB: {SRC_GLB}")

    OUT.mkdir(parents=True, exist_ok=True)
    parent_meta = Path(str(OUT.parent) + ".meta")
    if not parent_meta.exists():
        write_folder_meta(parent_meta, new_guid())
    folder_meta = Path(str(OUT) + ".meta")
    if not folder_meta.exists():
        write_folder_meta(folder_meta, new_guid())

    gltf, blob = load_glb(SRC_GLB)
    raw_images = extract_images(gltf, blob, OUT)

    # Mesh 1 = Asteroid_1 (identical topology for all three)
    mesh = gltf["meshes"][1]
    prim = mesh["primitives"][0]
    attrs = prim["attributes"]
    positions = read_accessor(gltf, blob, attrs["POSITION"])
    normals = read_accessor(gltf, blob, attrs["NORMAL"])
    uvs = read_accessor(gltf, blob, attrs["TEXCOORD_0"])
    indices = read_accessor(gltf, blob, prim["indices"]).astype(np.int32).ravel()

    obj_path = OUT / "AsteroidBall.obj"
    mesh_info = export_obj(positions, normals, uvs, indices, obj_path)
    obj_guid = new_guid()
    write_model_meta(Path(str(obj_path) + ".meta"), obj_guid)

    # Textures from GLB mapping (see convert notes).
    # image_0 albedo (grayscale rock), image_3 normal,
    # image_4 ORM for cold, image_1 ORM for warm/hot,
    # image_2 emissive warm, image_5 emissive hot.
    tex_specs = [
        ("Asteroid_albedo.png", raw_images[0], True, False),
        ("Asteroid_normal.png", raw_images[3], False, True),
        ("Asteroid_1_orm.png", raw_images[4], False, False),
        ("Asteroid_2_orm.png", raw_images[1], False, False),
        ("Asteroid_2_emissive.png", raw_images[2], True, False),
        ("Asteroid_3_emissive.png", raw_images[5], True, False),
    ]
    tex_guids = {}
    for name, src, srgb, normal in tex_specs:
        dest = OUT / name
        save_rgb(Image.open(src), dest)
        guid = new_guid()
        write_texture_meta(Path(str(dest) + ".meta"), guid, srgb=srgb, normal=normal)
        tex_guids[name] = guid

    for raw in raw_images:
        raw.unlink(missing_ok=True)

    attribution = OUT / "ATTRIBUTION.txt"
    attribution.write_text(
        """Free Asteroid Ball
Author: Lucas Patez (lukpatez) — https://sketchfab.com/lukpatez
Source: https://sketchfab.com/3d-models/free-asteroid-ball-7cd9225dd6f841638f6fa747fecbe260
License: Sketchfab Free Standard (https://sketchfab.com/licenses)
NoAI: may not be used in generative AI training/datasets.

Variants (identical mesh, different materials):
  Asteroid_1 — cold rock (far from Sun)
  Asteroid_2 — warm / lava cracks (intermediate)
  Asteroid_3 — hot / strong emissive (near Sun)
""",
        encoding="utf-8",
    )
    Path(str(attribution) + ".meta").write_text(
        f"""fileFormatVersion: 2
guid: {new_guid()}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )

    info = {
        "source": "free_asteroid_ball.glb",
        "sketchfab": "https://sketchfab.com/3d-models/free-asteroid-ball-7cd9225dd6f841638f6fa747fecbe260",
        "author": "Lucas Patez (lukpatez)",
        "license": "Sketchfab Free Standard",
        "mesh": mesh_info,
        "variants": {
            "Cold": {"material": "Asteroid_1", "orm": "Asteroid_1_orm.png", "emissive": None},
            "Warm": {
                "material": "Asteroid_2",
                "orm": "Asteroid_2_orm.png",
                "emissive": "Asteroid_2_emissive.png",
            },
            "Hot": {
                "material": "Asteroid_3",
                "orm": "Asteroid_2_orm.png",
                "emissive": "Asteroid_3_emissive.png",
            },
        },
        "shared": {
            "albedo": "Asteroid_albedo.png",
            "normal": "Asteroid_normal.png",
            "obj": "AsteroidBall.obj",
        },
        "textureGuids": tex_guids,
        "objGuid": obj_guid,
        "thresholdsAu": {"hotBelow": 0.8, "warmBelow": 2.2},
    }
    info_path = OUT / "mesh_info.json"
    info_path.write_text(json.dumps(info, indent=2), encoding="utf-8")
    Path(str(info_path) + ".meta").write_text(
        f"""fileFormatVersion: 2
guid: {new_guid()}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )

    print(f"Wrote {obj_path}")
    print(f"Triangles: {mesh_info['triangleCount']}, verts: {mesh_info['vertexCount']}")
    print(f"Textures: {', '.join(tex_guids)}")


if __name__ == "__main__":
    main()
