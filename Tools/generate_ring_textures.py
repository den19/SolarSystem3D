"""Generate JupiterRing_8k, UranusRing_8k, and NeptuneRing_8k radial ring atlases (U=angle, V=radial)."""
import math
import os
from PIL import Image

W, H = 4096, 1024
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "PlanetTexturesHD")


def radial_profile(y_norm, bands):
    """y_norm in [0,1] from inner to outer edge."""
    v = 0.0
    for inner, outer, peak, width in bands:
        if inner <= y_norm <= outer:
            center = (inner + outer) * 0.5
            d = abs(y_norm - center) / max(width, 1e-4)
            v = max(v, peak * math.exp(-d * d * 4.0))
    edge = min(y_norm, 1.0 - y_norm) * 12.0
    v *= min(1.0, edge)
    return v


def make_jupiter():
    # Warm dust: main band + gossamer halo
    bands = [
        (0.08, 0.22, 0.55, 0.06),
        (0.24, 0.38, 0.35, 0.05),
        (0.52, 0.72, 0.22, 0.12),
        (0.78, 0.95, 0.12, 0.08),
    ]
    img = Image.new("RGBA", (W, H))
    px = img.load()
    for y in range(H):
        yn = y / (H - 1)
        for x in range(W):
            ang = (x / W) * math.pi * 2.0
            noise = 0.92 + 0.08 * math.sin(ang * 47.0 + yn * 31.0)
            a = radial_profile(yn, bands) * noise
            r = int(210 * a + 25 * (1 - a))
            g = int(185 * a + 20 * (1 - a))
            b = int(155 * a + 15 * (1 - a))
            px[x, y] = (r, g, b, int(255 * min(1.0, a * 1.15)))
    return img


def make_uranus():
    # Cool icy narrow bands + gaps
    bands = [
        (0.05, 0.09, 0.7, 0.02),
        (0.11, 0.14, 0.15, 0.015),
        (0.16, 0.20, 0.85, 0.025),
        (0.22, 0.26, 0.2, 0.02),
        (0.28, 0.34, 0.75, 0.03),
        (0.36, 0.42, 0.25, 0.025),
        (0.44, 0.52, 0.65, 0.04),
        (0.55, 0.62, 0.3, 0.03),
        (0.64, 0.78, 0.5, 0.07),
        (0.82, 0.96, 0.35, 0.06),
    ]
    img = Image.new("RGBA", (W, H))
    px = img.load()
    for y in range(H):
        yn = y / (H - 1)
        for x in range(W):
            ang = (x / W) * math.pi * 2.0
            noise = 0.94 + 0.06 * math.sin(ang * 61.0 + yn * 19.0)
            a = radial_profile(yn, bands) * noise
            r = int(175 * a + 12 * (1 - a))
            g = int(210 * a + 18 * (1 - a))
            b = int(235 * a + 28 * (1 - a))
            px[x, y] = (r, g, b, int(255 * min(1.0, a * 1.2)))
    return img


def adams_arc_boost(ang):
    """Three localized bright arcs on the Adams ring (Liberté, Egalité, Fraternité)."""
    centers = (0.35, 2.45, 4.55)
    boost = 0.0
    for c in centers:
        d = abs(math.atan2(math.sin(ang - c), math.cos(ang - c)))
        boost = max(boost, math.exp(-(d * d) / 0.018))
    return 0.55 + 0.45 * boost


def make_neptune():
    # Faint dusty narrow rings + Adams arcs (cool blue-gray)
    bands = [
        (0.06, 0.10, 0.42, 0.018),
        (0.12, 0.16, 0.12, 0.012),
        (0.18, 0.22, 0.48, 0.016),
        (0.26, 0.30, 0.14, 0.012),
        (0.34, 0.40, 0.38, 0.02),
        (0.44, 0.50, 0.16, 0.014),
        (0.56, 0.64, 0.32, 0.022),
        (0.72, 0.88, 0.55, 0.035),
    ]
    img = Image.new("RGBA", (W, H))
    px = img.load()
    for y in range(H):
        yn = y / (H - 1)
        for x in range(W):
            ang = (x / W) * math.pi * 2.0
            noise = 0.93 + 0.07 * math.sin(ang * 53.0 + yn * 23.0)
            a = radial_profile(yn, bands) * noise
            if yn >= 0.68:
                a *= adams_arc_boost(ang)
            r = int(95 * a + 8 * (1 - a))
            g = int(118 * a + 10 * (1 - a))
            b = int(165 * a + 14 * (1 - a))
            px[x, y] = (r, g, b, int(255 * min(1.0, a * 1.1)))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    make_jupiter().save(os.path.join(OUT, "JupiterRing_8k.png"), "PNG")
    make_uranus().save(os.path.join(OUT, "UranusRing_8k.png"), "PNG")
    make_neptune().save(os.path.join(OUT, "NeptuneRing_8k.png"), "PNG")
    print("Wrote JupiterRing_8k.png, UranusRing_8k.png, and NeptuneRing_8k.png to", OUT)


if __name__ == "__main__":
    main()
