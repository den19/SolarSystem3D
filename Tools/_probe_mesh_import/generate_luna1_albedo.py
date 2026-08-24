"""
Generate Luna-1 body albedo: golden metallic sphere + equatorial USSR band.

Output: Assets/Resources/ProbeMeshes/Luna1/Luna1_albedo.png
Spherical UV: u wraps longitude, v=0.5 is equator (red band + hammer/sickle).
"""
from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Resources" / "ProbeMeshes" / "Luna1" / "Luna1_albedo.png"
SIZE = 512

# Warm brass/gold base for metallic probe body
GOLD_BASE = np.array([215, 178, 82], dtype=np.float32)
GOLD_HI = np.array([240, 205, 110], dtype=np.float32)
GOLD_LO = np.array([175, 138, 58], dtype=np.float32)
USSR_RED = (196, 22, 32)
USSR_GOLD = (255, 210, 45)
USSR_GOLD_OUTLINE = (120, 30, 28)  # dark red stroke for contrast on gold body
BAND_V0, BAND_V1 = 0.44, 0.56
# Cyrillic СССР (Latin CCCP is the common transliteration)
INSCRIPTION = "СССР"

_FONT_CANDIDATES = (
    Path(r"C:\Windows\Fonts\arialbd.ttf"),
    Path(r"C:\Windows\Fonts\segoeuib.ttf"),
    Path(r"C:\Windows\Fonts\courbd.ttf"),
    Path(r"C:\Windows\Fonts\arial.ttf"),
)


def _noise(x: np.ndarray, y: np.ndarray, seed: float = 1.7) -> np.ndarray:
    return (
        np.sin(x * 12.9898 + y * 78.233 + seed) * 43758.5453 % 1.0
        + np.sin(x * 39.346 + y * 11.135 + seed * 2.1) * 22578.1459 % 1.0
    ) * 0.5


def build_gold_sphere() -> np.ndarray:
    h, w = SIZE, SIZE
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    u = xs / (w - 1)
    v = 1.0 - ys / (h - 1)  # image top = v=1 (north pole)

    n = _noise(u * 80.0, v * 40.0)
    n2 = _noise(u * 160.0 + 3.1, v * 80.0 + 1.3, seed=4.2)
    mix = np.clip(n * 0.55 + n2 * 0.45, 0.0, 1.0)

    rgb = GOLD_LO + (GOLD_HI - GOLD_LO) * mix[..., None]
    # Subtle longitudinal brushing for machined metal look
    brush = 0.92 + 0.08 * np.sin(u * math.tau * 6.0)
    rgb *= brush[..., None]

    img = np.clip(rgb, 0, 255).astype(np.uint8)
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., :3] = img
    rgba[..., 3] = 255
    return rgba


def apply_equatorial_band(rgba: np.ndarray) -> None:
    h, w = rgba.shape[:2]
    y0 = int((1.0 - BAND_V1) * (h - 1))
    y1 = int((1.0 - BAND_V0) * (h - 1))
    rgba[y0 : y1 + 1, :, 0] = USSR_RED[0]
    rgba[y0 : y1 + 1, :, 1] = USSR_RED[1]
    rgba[y0 : y1 + 1, :, 2] = USSR_RED[2]


def _draw_hammer_sickle(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float) -> None:
    """Bold Soviet emblem — readable at phone scale on ~1.6 m probe."""
    s = scale
    gold = USSR_GOLD

    # Sickle — curved blade + handle
    sickle_pts = [
        (cx + 0.55 * s, cy - 0.05 * s),
        (cx + 0.35 * s, cy + 0.55 * s),
        (cx - 0.05 * s, cy + 0.75 * s),
        (cx - 0.25 * s, cy + 0.55 * s),
        (cx + 0.05 * s, cy + 0.15 * s),
        (cx + 0.35 * s, cy - 0.15 * s),
    ]
    draw.polygon(sickle_pts, fill=gold)
    draw.line(
        [(cx - 0.15 * s, cy + 0.65 * s), (cx - 0.55 * s, cy + 1.05 * s)],
        fill=gold,
        width=max(3, int(0.12 * s)),
    )

    # Hammer head
    head = [
        (cx - 0.95 * s, cy - 0.35 * s),
        (cx + 0.15 * s, cy - 0.35 * s),
        (cx + 0.15 * s, cy - 0.05 * s),
        (cx - 0.95 * s, cy - 0.05 * s),
    ]
    draw.polygon(head, fill=gold)
    # Hammer handle
    draw.line(
        [(cx - 0.55 * s, cy - 0.05 * s), (cx - 0.55 * s, cy + 0.85 * s)],
        fill=gold,
        width=max(3, int(0.14 * s)),
    )

    # Star above emblem (five-point, simplified)
    star_r = 0.22 * s
    star_cy = cy - 0.75 * s
    star_pts = []
    for i in range(10):
        ang = math.radians(-90 + i * 36)
        r = star_r if i % 2 == 0 else star_r * 0.42
        star_pts.append((cx + r * math.cos(ang), star_cy + r * math.sin(ang)))
    draw.polygon(star_pts, fill=gold)


def _load_bold_cyrillic_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for path in _FONT_CANDIDATES:
        if path.is_file():
            try:
                return ImageFont.truetype(str(path), size=size)
            except OSError:
                continue
    return ImageFont.load_default()


def _draw_inscription(draw: ImageDraw.ImageDraw, cx: int, cy: int, scale: float) -> None:
    """Bold СССР under the emblem — readable at phone scale after FitToBounds."""
    font_px = max(28, int(0.72 * scale))
    font = _load_bold_cyrillic_font(font_px)
    text = INSCRIPTION
    # Anchor below hammer/sickle handles (handles end ~cy + 1.05*s)
    ty = int(cy + 1.35 * scale)
    # Dark outline then gold fill for contrast on metallic gold
    stroke = max(2, font_px // 10)
    draw.text(
        (cx, ty),
        text,
        font=font,
        fill=USSR_GOLD,
        stroke_width=stroke,
        stroke_fill=USSR_GOLD_OUTLINE,
        anchor="mt",
    )


def apply_emblem(rgba: np.ndarray) -> None:
    img = Image.fromarray(rgba)
    draw = ImageDraw.Draw(img)
    cx, cy = SIZE // 2, int((1.0 - 0.5) * (SIZE - 1))
    scale = SIZE * 0.11
    _draw_hammer_sickle(draw, cx, cy, scale=scale)
    _draw_inscription(draw, cx, cy, scale=scale)
    rgba[:] = np.array(img)


def main() -> int:
    rgba = build_gold_sphere()
    apply_equatorial_band(rgba)
    apply_emblem(rgba)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba).save(OUT)
    # ASCII-safe log (Windows consoles may be cp1252)
    print("Wrote", OUT, SIZE, "x", SIZE, "inscription=CCCP/SSSR (Cyrillic)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
