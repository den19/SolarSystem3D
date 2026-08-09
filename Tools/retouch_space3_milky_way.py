#!/usr/bin/env python3
"""Retouch SBS Space 3 Medium skybox faces toward a cooler Milky Way look.

- Shift warm orange nebula toward silver/cream with faint magenta
- Darken a dust lane along the horizontal mid-band
- Soften the circular eclipse/corona artifact on Left_Medium.png
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image

TEX_DIR = Path(__file__).resolve().parents[1] / (
    "Assets/Free Skyboxes - Space/SBS Space 3/Medium/Textures"
)
FACES = [
    "Front_Medium.png",
    "Back_Medium.png",
    "Left_Medium.png",
    "Right_Medium.png",
    "Up_Medium.png",
    "Down_Medium.png",
]


def load_rgb(path: Path) -> np.ndarray:
    return np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)


def save_rgb(path: Path, arr: np.ndarray) -> None:
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB").save(path, optimize=True)


def star_mask(rgb: np.ndarray) -> np.ndarray:
    """Bright near-neutral pixels treated as stars (preserve them)."""
    lum = rgb.mean(axis=2)
    mx = rgb.max(axis=2)
    mn = rgb.min(axis=2)
    chroma = mx - mn
    return (lum > 140.0) & (chroma < 45.0)


def cool_nebula(rgb: np.ndarray) -> np.ndarray:
    """Map warm orange haze toward cool silver/cream MW tones."""
    out = rgb.copy()
    r, g, b = out[..., 0], out[..., 1], out[..., 2]
    warm = np.clip((r - b) / 180.0, 0.0, 1.0) * np.clip((r - 40.0) / 160.0, 0.0, 1.0)
    warm *= 1.0 - star_mask(rgb).astype(np.float32)

    # Desaturate orange toward luminance, then bias cool silver/cream.
    lum = 0.30 * r + 0.59 * g + 0.11 * b
    target = np.stack(
        [
            lum * 1.02 + 14.0,  # cream
            lum * 1.04 + 12.0,
            lum * 1.18 + 28.0,  # cool lift
        ],
        axis=-1,
    )
    # Soft magenta only in denser warm cores (subtle, not brown).
    magenta = np.stack(
        [lum * 0.08, lum * -0.02, lum * 0.10],
        axis=-1,
    )
    blend = warm[..., None]
    out = out * (1.0 - 0.88 * blend) + (target + magenta) * (0.88 * blend)
    return out


def dust_lane(rgb: np.ndarray, strength: float = 1.0) -> np.ndarray:
    """Darken a horizontal Great-Rift-like lane near image mid-height."""
    h, w = rgb.shape[:2]
    yy = np.linspace(-1.0, 1.0, h, dtype=np.float32)[:, None]
    # Primary lane near equator; slight warp so it is not a hard ruler line.
    x = np.linspace(-1.0, 1.0, w, dtype=np.float32)[None, :]
    lane_y = 0.02 * np.sin(x * math.pi * 1.5)
    dist = np.abs(yy - lane_y)
    lane = np.exp(-((dist / 0.085) ** 2))
    # Stronger darkening where there is already nebula (warm/bright dust).
    lum = rgb.mean(axis=2) / 255.0
    neb = np.clip((lum - 0.12) / 0.45, 0.0, 1.0)
    stars = star_mask(rgb).astype(np.float32)
    amount = lane * neb * (1.0 - stars) * (0.55 * strength)
    factor = 1.0 - amount
    # Dust is slightly brown, not pure black.
    brown = np.array([0.75, 0.70, 0.68], dtype=np.float32)
    out = rgb * factor[..., None]
    out += (rgb * brown) * (amount * 0.35)[..., None]
    return out


def remove_left_eclipse(rgb: np.ndarray, donor: np.ndarray) -> np.ndarray:
    """Replace Left sun/eclipse disk with Front-face band content + soft blend."""
    h, w = rgb.shape[:2]
    cx, cy = 1020, 800
    hard_r, soft_r = 280, 360

    yy, xx = np.mgrid[0:h, 0:w]
    rr = np.sqrt((yy - cy) ** 2 + (xx - cx) ** 2).astype(np.float32)

    # Donor: Front mid-band, horizontally mirrored so seams stay soft.
    filled = donor.copy()
    # Slight vertical nudge so galactic center reads denser, not a pasted sun.
    shift = 40
    filled = np.roll(filled, shift, axis=0)

    amount = np.clip(1.0 - (rr - hard_r) / max(1.0, soft_r - hard_r), 0.0, 1.0)
    amount = np.where(rr <= hard_r, 1.0, amount * amount)

    # Prefer Left's own side-band colors at the same Y for continuity.
    y0, y1 = int(h * 0.28), int(h * 0.52)
    left_strip = rgb[y0:y1, 40:220].mean(axis=1)
    right_strip = rgb[y0:y1, w - 220 : w - 40].mean(axis=1)
    profile = 0.5 * (left_strip + right_strip)
    band_idx = np.clip(
        ((yy - y0) / max(1.0, float(y1 - y0 - 1)) * (profile.shape[0] - 1)).astype(np.int32),
        0,
        profile.shape[0] - 1,
    )
    band = profile[band_idx]
    # Mix donor structure with local band color.
    hybrid = 0.55 * filled + 0.45 * band

    rng = np.random.default_rng(11)
    noise = rng.normal(0.0, 4.0, size=rgb.shape).astype(np.float32)
    stars = (rng.random((h, w)) > 0.996).astype(np.float32)
    star_col = np.array([218.0, 222.0, 232.0], dtype=np.float32)
    hybrid = np.clip(hybrid + noise, 0, 255)
    hybrid = hybrid * (1.0 - stars[..., None]) + star_col * stars[..., None]

    return rgb * (1.0 - amount[..., None]) + hybrid * amount[..., None]


def process_face(name: str, rgb: np.ndarray, donor_front: np.ndarray | None = None) -> np.ndarray:
    out = cool_nebula(rgb)
    if name == "Left_Medium.png":
        if donor_front is None:
            raise SystemExit("Left face requires Front donor")
        out = remove_left_eclipse(out, cool_nebula(donor_front))
    # Up/Down get a weaker dust treatment (poles).
    if name in ("Up_Medium.png", "Down_Medium.png"):
        out = dust_lane(out, strength=0.25)
    else:
        out = dust_lane(out, strength=1.0)
    # Mild overall exposure trim so MW is not neon.
    out = out * 0.96
    return out


def main() -> None:
    if not TEX_DIR.is_dir():
        raise SystemExit(f"Texture dir not found: {TEX_DIR}")

    front_src = load_rgb(TEX_DIR / "Front_Medium.png")

    for name in FACES:
        path = TEX_DIR / name
        if not path.is_file():
            raise SystemExit(f"Missing: {path}")
        src = load_rgb(path)
        dst = process_face(name, src, donor_front=front_src)
        save_rgb(path, dst)
        print(f"Retouched {name}")

    print("Done.")


if __name__ == "__main__":
    main()
