#!/usr/bin/env python3
"""Compose 16:9 RuStore posters from Probe screenshots in Оформление2 style."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
SHOTS = ROOT / "APK_Screenshots_24082026"
OUT_W, OUT_H = 1920, 1080
FONT_PATH = Path(r"C:\Windows\Fonts\arialbd.ttf")
FONT_FALLBACK = Path(r"C:\Windows\Fonts\segoeuib.ttf")

# Screenshots are 1920x864 — cover into 1920x1080 needs zoom>1 to allow vertical reframing.
# crop_bias_y: 0 = top, 1 = bottom of the scaled source.
POSTERS = [
    {
        # Flight + telemetry — caption upper-center like «ИССЛЕДОВАТЕЛЬСКИЕ РЕЖИМЫ».
        # Zoom + top bias crops the cramped bottom control bar out of frame.
        "src": "428a3784-fe04-446f-95c6-9ad5c5c89b76.jpg",
        "out": "7_RezhimZond.png",
        "lines": ["РЕЖИМ ЗОНД"],
        "anchor": "center",
        "margin_x": 0,
        "margin_y": 96,
        "font_size": 124,
        "line_gap": 18,
        "crop_bias_y": 0.05,
        "zoom": 1.75,
    },
    {
        # Mars-3 close-up — two-line caption in upper third (not over the craft).
        "src": "c8765c55-65f5-4ae1-853e-38c5ced63891.jpg",
        "out": "8_IstoricheskieMissii.png",
        "lines": ["ИСТОРИЧЕСКИЕ", "МИССИИ"],
        "anchor": "center",
        "margin_x": 0,
        "margin_y": 64,
        "font_size": 112,
        "line_gap": 8,
        "crop_bias_y": 0.15,
        "zoom": 1.55,
    },
]


def load_font(size: int) -> ImageFont.FreeTypeFont:
    path = FONT_PATH if FONT_PATH.exists() else FONT_FALLBACK
    return ImageFont.truetype(str(path), size=size)


def fit_cover(
    src: Image.Image,
    tw: int,
    th: int,
    bias_y: float = 0.5,
    zoom: float = 1.0,
) -> Image.Image:
    """Scale to cover target (with optional zoom), crop with vertical bias."""
    sw, sh = src.size
    scale = max(tw / sw, th / sh) * max(1.0, zoom)
    nw, nh = int(round(sw * scale)), int(round(sh * scale))
    resized = src.resize((nw, nh), Image.Resampling.LANCZOS)
    left = max(0, (nw - tw) // 2)
    max_top = max(0, nh - th)
    top = int(round(max_top * max(0.0, min(1.0, bias_y))))
    return resized.crop((left, top, left + tw, top + th))


def draw_caption(
    canvas: Image.Image,
    lines: list[str],
    *,
    anchor: str,
    margin_x: int,
    margin_y: int,
    font_size: int,
    line_gap: int,
) -> None:
    """Large white ALL-CAPS caption — matches Оформление2 (no outline/glow)."""
    draw = ImageDraw.Draw(canvas)
    font = load_font(font_size)

    sizes = [draw.textbbox((0, 0), line, font=font) for line in lines]
    heights = [b[3] - b[1] for b in sizes]
    widths = [b[2] - b[0] for b in sizes]
    block_h = sum(heights) + line_gap * (len(lines) - 1)
    block_w = max(widths) if widths else 0

    if anchor == "right":
        x0 = OUT_W - margin_x - block_w
    elif anchor == "center":
        x0 = (OUT_W - block_w) // 2
    else:
        x0 = margin_x

    y = margin_y
    if y + block_h > OUT_H - 80:
        y = max(60, OUT_H - 80 - block_h)

    fill = (255, 255, 255, 255)

    y_cursor = y
    for i, line in enumerate(lines):
        w = widths[i]
        if anchor == "right":
            x = OUT_W - margin_x - w
        elif anchor == "center":
            x = (OUT_W - w) // 2
        else:
            x = x0
        draw.text((x, y_cursor), line, font=font, fill=fill)
        y_cursor += heights[i] + line_gap


def make_poster(spec: dict) -> Path:
    src_path = SHOTS / spec["src"]
    src = Image.open(src_path).convert("RGBA")
    canvas = fit_cover(
        src,
        OUT_W,
        OUT_H,
        bias_y=float(spec.get("crop_bias_y", 0.5)),
        zoom=float(spec.get("zoom", 1.0)),
    )
    draw_caption(
        canvas,
        spec["lines"],
        anchor=spec["anchor"],
        margin_x=spec["margin_x"],
        margin_y=spec["margin_y"],
        font_size=spec["font_size"],
        line_gap=spec["line_gap"],
    )
    out_path = ROOT / spec["out"]
    canvas.convert("RGBA").save(out_path, "PNG", optimize=True)
    return out_path


def main() -> None:
    for spec in POSTERS:
        path = make_poster(spec)
        im = Image.open(path)
        print(f"Wrote {path.name}: {im.size[0]}x{im.size[1]}")


if __name__ == "__main__":
    main()
