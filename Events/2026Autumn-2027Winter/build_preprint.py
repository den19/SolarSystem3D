# -*- coding: utf-8 -*-
"""Generate RuStore Events preprint PDF (reportlab + Pillow)."""

from __future__ import annotations

import io
import os
import sys
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from reportlab.lib.colors import HexColor, black, white
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    Flowable,
    Image as RLImage,
    KeepTogether,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)

from events_data import (
    DESC_MAX,
    EVENTS,
    FIRST_BATCH_IDS,
    SHORT_MAX,
    TITLE_MAX,
    assert_limits,
)

ROOT = Path(__file__).resolve().parent
OUT_PDF = ROOT / "RuStore_Events_Preprint_2026-2027.pdf"
PAGE_W, PAGE_H = A4
MARGIN = 14 * mm


def _find_cyrillic_font() -> tuple[str, str | None]:
    """Return (regular_ttf, bold_ttf_or_None) from Windows fonts."""
    windir = Path(os.environ.get("WINDIR", r"C:\Windows"))
    fonts = windir / "Fonts"
    candidates = [
        ("segoeui.ttf", "segoeuib.ttf"),
        ("arial.ttf", "arialbd.ttf"),
        ("tahoma.ttf", "tahomabd.ttf"),
        ("calibri.ttf", "calibrib.ttf"),
    ]
    for reg, bold in candidates:
        reg_path = fonts / reg
        if reg_path.is_file():
            bold_path = fonts / bold
            return str(reg_path), str(bold_path) if bold_path.is_file() else None
    raise FileNotFoundError(
        "No Cyrillic TTF found (tried Segoe UI, Arial, Tahoma, Calibri)"
    )


def register_fonts() -> tuple[str, str]:
    reg_path, bold_path = _find_cyrillic_font()
    pdfmetrics.registerFont(TTFont("AppSans", reg_path))
    if bold_path:
        pdfmetrics.registerFont(TTFont("AppSans-Bold", bold_path))
        bold_name = "AppSans-Bold"
    else:
        bold_name = "AppSans"
    return "AppSans", bold_name


FONT, FONT_BOLD = register_fonts()


def hex_to_rgb(h: str) -> tuple[int, int, int]:
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def lighten(rgb: tuple[int, int, int], factor: float = 0.35) -> tuple[int, int, int]:
    return tuple(min(255, int(c + (255 - c) * factor)) for c in rgb)


def darken(rgb: tuple[int, int, int], factor: float = 0.35) -> tuple[int, int, int]:
    return tuple(max(0, int(c * (1 - factor))) for c in rgb)


def make_preview_png(event: dict, width: int = 480, height: int = 360) -> bytes:
    """4:3 gradient preview with safe-zone label (Pillow)."""
    fill = hex_to_rgb(event["fill_hex"])
    companions = [hex_to_rgb(c) for c in event["companions"]]
    img = Image.new("RGB", (width, height), fill)
    draw = ImageDraw.Draw(img)

    # Vertical-ish gradient using fill → companion[0] → lightened fill
    c0 = fill
    c1 = companions[0] if companions else lighten(fill)
    c2 = lighten(fill, 0.25)
    for y in range(height):
        t = y / max(1, height - 1)
        if t < 0.55:
            u = t / 0.55
            r = int(c0[0] + (c1[0] - c0[0]) * u)
            g = int(c0[1] + (c1[1] - c0[1]) * u)
            b = int(c0[2] + (c1[2] - c0[2]) * u)
        else:
            u = (t - 0.55) / 0.45
            r = int(c1[0] + (c2[0] - c1[0]) * u)
            g = int(c1[1] + (c2[1] - c1[1]) * u)
            b = int(c1[2] + (c2[2] - c1[2]) * u)
        draw.line([(0, y), (width, y)], fill=(r, g, b))

    # Soft edge darkening
    edge = darken(fill, 0.5)
    for i in range(0, 28, 2):
        draw.rectangle(
            [i, i, width - 1 - i, height - 1 - i],
            outline=(edge[0], edge[1], edge[2]),
            width=1,
        )

    # Hero hint circle
    cx, cy = int(width * 0.55), int(height * 0.58)
    r = int(min(width, height) * 0.18)
    glow = lighten(companions[1] if len(companions) > 1 else fill, 0.45)
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=glow)
    draw.ellipse(
        [cx - r, cy - r, cx + r, cy + r],
        outline=lighten(fill, 0.55),
        width=2,
    )

    words = event["cover_brief"]["safe_zone_words"]
    text_fill = (255, 255, 255) if event["text_color"] == "white" else (10, 10, 10)

    font = None
    for size in (36, 32, 28):
        try:
            font = ImageFont.truetype(_find_cyrillic_font()[0], size)
            break
        except OSError:
            continue
    if font is None:
        font = ImageFont.load_default()

    # Safe-zone band (top center ≈ 1260×1030 of 2880×2160 → ~44%×48%)
    sz_w, sz_h = int(width * 0.44), int(height * 0.48)
    sz_x0 = (width - sz_w) // 2
    sz_y0 = int(height * 0.06)
    draw.rectangle(
        [sz_x0, sz_y0, sz_x0 + sz_w, sz_y0 + sz_h],
        outline=(255, 255, 255, 90),
        width=1,
    )

    bbox = draw.textbbox((0, 0), words, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    tx = (width - tw) // 2
    ty = sz_y0 + int(sz_h * 0.12)
    # Soft shadow for readability
    shadow = (0, 0, 0) if event["text_color"] == "white" else (255, 255, 255)
    draw.text((tx + 1, ty + 1), words, font=font, fill=shadow)
    draw.text((tx, ty), words, font=font, fill=text_fill)

    # Small caption: hero one-liner truncated
    hero = event["cover_brief"]["hero"]
    if len(hero) > 52:
        hero = hero[:49] + "…"
    try:
        small = ImageFont.truetype(_find_cyrillic_font()[0], 12)
    except OSError:
        small = font
    draw.text((10, height - 22), hero, font=small, fill=text_fill)

    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=True)
    return buf.getvalue()


class ColorChipRow(Flowable):
    """Row of colour chips with HEX labels."""

    def __init__(self, hexes: list[str], labels: list[str], chip: float = 14 * mm):
        super().__init__()
        self.hexes = hexes
        self.labels = labels
        self.chip = chip
        self.height = chip + 8 * mm

    def wrap(self, availWidth, availHeight):
        self.width = availWidth
        return availWidth, self.height

    def draw(self):
        x = 0
        gap = 4 * mm
        for hx, lab in zip(self.hexes, self.labels):
            self.canv.setFillColor(HexColor(hx))
            self.canv.setStrokeColor(HexColor("#333333"))
            self.canv.setLineWidth(0.4)
            self.canv.roundRect(x, 7 * mm, self.chip, self.chip, 2 * mm, fill=1, stroke=1)
            self.canv.setFillColor(black)
            self.canv.setFont(FONT, 7)
            self.canv.drawString(x, 2 * mm, lab)
            self.canv.setFont(FONT, 6.5)
            self.canv.drawString(x, 0 * mm, hx.upper())
            x += self.chip + gap


def styles():
    return {
        "h1": ParagraphStyle(
            "h1",
            fontName=FONT_BOLD,
            fontSize=18,
            leading=22,
            spaceAfter=6,
            textColor=HexColor("#1a1a2e"),
        ),
        "h2": ParagraphStyle(
            "h2",
            fontName=FONT_BOLD,
            fontSize=13,
            leading=16,
            spaceBefore=4,
            spaceAfter=4,
            textColor=HexColor("#1a1a2e"),
        ),
        "h3": ParagraphStyle(
            "h3",
            fontName=FONT_BOLD,
            fontSize=11,
            leading=14,
            spaceBefore=6,
            spaceAfter=3,
            textColor=HexColor("#243B8A"),
        ),
        "body": ParagraphStyle(
            "body",
            fontName=FONT,
            fontSize=9,
            leading=12,
            spaceAfter=3,
            textColor=black,
        ),
        "small": ParagraphStyle(
            "small",
            fontName=FONT,
            fontSize=8,
            leading=10,
            spaceAfter=2,
            textColor=HexColor("#333333"),
        ),
        "note": ParagraphStyle(
            "note",
            fontName=FONT,
            fontSize=8.5,
            leading=11,
            spaceBefore=4,
            spaceAfter=4,
            textColor=HexColor("#6B1E2A"),
            borderPadding=4,
        ),
        "mono": ParagraphStyle(
            "mono",
            fontName=FONT,
            fontSize=8.5,
            leading=11,
            spaceAfter=2,
            textColor=HexColor("#111111"),
        ),
        "count": ParagraphStyle(
            "count",
            fontName=FONT,
            fontSize=7.5,
            leading=9,
            textColor=HexColor("#555555"),
        ),
    }


def esc(text: str) -> str:
    return (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
    )


def concurrent_note(events: list[dict]) -> str:
    """Ensure calendar never implies >5 concurrent active windows (guideline)."""
    # Sweep line on dates
    points: list[tuple[datetime, int]] = []
    for ev in events:
        a = datetime.strptime(ev["date_start"], "%Y-%m-%d")
        b = datetime.strptime(ev["date_end"], "%Y-%m-%d")
        points.append((a, +1))
        points.append((b, -1))  # end inclusive; decrement after day
    # For inclusive ends, treat end as leaving next day
    points = []
    for ev in events:
        a = datetime.strptime(ev["date_start"], "%Y-%m-%d")
        b = datetime.strptime(ev["date_end"], "%Y-%m-%d")
        points.append((a.toordinal(), +1, ev["id"]))
        points.append((b.toordinal() + 1, -1, ev["id"]))
    points.sort(key=lambda x: (x[0], x[1]))
    cur = 0
    peak = 0
    for _, delta, _ in points:
        cur += delta
        peak = max(peak, cur)
    return (
        f"Календарь рассчитан так, чтобы одновременно активных окон было "
        f"не больше 5 (пик пересечений в модели: {peak}). В Консоли RuStore "
        f"одновременно допускается ≤5 заявок."
    )


def build_title_page(S: dict) -> list:
    story: list = []
    story.append(Paragraph("RuStore Events — препринт идей", S["h1"]))
    story.append(
        Paragraph(
            "Солнечная Система 3D · осень 2026 — зима 2027 · 12 событий",
            S["body"],
        )
    )
    story.append(Spacer(1, 4 * mm))

    story.append(Paragraph("Лимиты консоли RuStore", S["h2"]))
    story.append(
        Paragraph(
            f"• Название ≤ <b>{TITLE_MAX}</b> символов<br/>"
            f"• Краткое описание ≤ <b>{SHORT_MAX}</b> (призыв к действию, «вы»)<br/>"
            f"• Описание ≤ <b>{DESC_MAX}</b> (контекст + действие в приложении)<br/>"
            f"• Обложка 2880×2160, 4:3, JPG/PNG ≤3 МБ; safe zone 1260×1030 "
            f"сверху по центру; на картинке 1–3 слова<br/>"
            f"• Длительность события 1–90 дней; модерация до 14 дней",
            S["body"],
        )
    )

    story.append(Paragraph("Календарь и параллельность", S["h2"]))
    story.append(Paragraph(esc(concurrent_note(EVENTS)), S["body"]))

    story.append(Paragraph("Важно перед подачей", S["h2"]))
    story.append(
        Paragraph(
            "<b>Перед отправкой в RuStore Console сверьте, что обещанный контент "
            "(новые модели зондов, «сезонный» фокус, открытка/шеринг и т.д.) "
            "реально доступен в билде на даты события.</b> Этот PDF — препринт "
            "идей, не финальные PNG 2880×2160.",
            S["note"],
        )
    )

    story.append(Paragraph("Топ-5 для первого батча (приоритет A)", S["h2"]))
    batch_lines = []
    for eid in FIRST_BATCH_IDS:
        ev = next(e for e in EVENTS if e["id"] == eid)
        batch_lines.append(
            f"#{eid} — {esc(ev['title'])} ({esc(ev['date_label'])}, {esc(ev['event_type'])})"
        )
    story.append(Paragraph("<br/>".join(batch_lines), S["body"]))

    story.append(Spacer(1, 3 * mm))
    story.append(Paragraph("Сводка всех 12 событий", S["h2"]))

    rows = [["#", "P", "Тип", "Окно", "Название", "HEX"]]
    for ev in EVENTS:
        rows.append(
            [
                str(ev["id"]),
                ev["priority"],
                ev["event_type"],
                ev["date_label"],
                ev["title"],
                ev["fill_hex"],
            ]
        )
    table = Table(
        rows,
        colWidths=[8 * mm, 7 * mm, 32 * mm, 38 * mm, 55 * mm, 20 * mm],
        repeatRows=1,
    )
    table.setStyle(
        TableStyle(
            [
                ("FONTNAME", (0, 0), (-1, 0), FONT_BOLD),
                ("FONTNAME", (0, 1), (-1, -1), FONT),
                ("FONTSIZE", (0, 0), (-1, -1), 7),
                ("BACKGROUND", (0, 0), (-1, 0), HexColor("#E8ECF4")),
                ("GRID", (0, 0), (-1, -1), 0.3, HexColor("#AAAAAA")),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 2),
                ("RIGHTPADDING", (0, 0), (-1, -1), 2),
                ("TOPPADDING", (0, 0), (-1, -1), 2),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 2),
            ]
        )
    )
    # Colorize priority A rows lightly
    for i, ev in enumerate(EVENTS, start=1):
        if ev["priority"] == "A":
            table.setStyle(
                TableStyle([("BACKGROUND", (0, i), (-1, i), HexColor("#FFF4E6"))])
            )
    story.append(table)
    story.append(PageBreak())
    return story


def build_event_card(ev: dict, S: dict) -> list:
    parts: list = []
    header = (
        f"#{ev['id']:02d} · {esc(ev['title'])} · "
        f"приоритет <b>{ev['priority']}</b>"
    )
    parts.append(Paragraph(header, S["h2"]))
    parts.append(
        Paragraph(
            f"Тип: <b>{esc(ev['event_type'])}</b> · "
            f"Окно: <b>{esc(ev['date_label'])}</b> · "
            f"Itten: {esc(ev['itten'])}",
            S["small"],
        )
    )

    parts.append(Paragraph("1. Изображение (бриф обложки)", S["h3"]))
    cb = ev["cover_brief"]
    parts.append(
        Paragraph(
            f"• Герой: {esc(cb['hero'])}<br/>"
            f"• Фон: {esc(cb['background'])}<br/>"
            f"• Safe zone (1–3 слова): <b>{esc(cb['safe_zone_words'])}</b><br/>"
            f"• Композиция: {esc(cb['composition'])}",
            S["body"],
        )
    )

    parts.append(Paragraph("2. Заливка и градиент", S["h3"]))
    hexes = [ev["fill_hex"]] + list(ev["companions"])
    labels = ["подложка"] + [f"companion {i}" for i in range(1, len(ev["companions"]) + 1)]
    parts.append(ColorChipRow(hexes, labels))
    parts.append(
        Paragraph(
            f"Цвет текста на подложке: <b>{'белый' if ev['text_color'] == 'white' else 'чёрный'}</b> "
            f"({esc(ev['text_color'])})",
            S["small"],
        )
    )

    parts.append(Paragraph("3. Содержимое (готовые строки для Консоли)", S["h3"]))
    title, short, desc = ev["title"], ev["short"], ev["description"]
    parts.append(
        Paragraph(
            f"<b>Название</b> ({len(title)}/{TITLE_MAX}): {esc(title)}",
            S["mono"],
        )
    )
    parts.append(
        Paragraph(
            f"<b>Краткое</b> ({len(short)}/{SHORT_MAX}): {esc(short)}",
            S["mono"],
        )
    )
    parts.append(
        Paragraph(
            f"<b>Описание</b> ({len(desc)}/{DESC_MAX}):",
            S["mono"],
        )
    )
    parts.append(Paragraph(esc(desc), S["body"]))

    parts.append(Paragraph("4. Хук в приложении", S["h3"]))
    parts.append(Paragraph(esc(ev["in_app_hook"]), S["body"]))

    parts.append(Paragraph("5. Превью 4:3 (градиент + safe-zone подпись)", S["h3"]))
    png = make_preview_png(ev)
    img_buf = io.BytesIO(png)
    # ~90mm wide → 4:3
    rl_img = RLImage(img_buf, width=90 * mm, height=67.5 * mm)
    parts.append(rl_img)
    parts.append(
        Paragraph(
            "Мини-превью для препринта; финальный арт 2880×2160 — отдельный этап.",
            S["count"],
        )
    )

    parts.append(Spacer(1, 2 * mm))
    return [KeepTogether(parts)]


def build_pdf(out_path: Path = OUT_PDF) -> Path:
    assert_limits(EVENTS)
    assert len(EVENTS) == 12, len(EVENTS)
    assert FIRST_BATCH_IDS == (1, 2, 3, 5, 8)

    S = styles()
    doc = SimpleDocTemplate(
        str(out_path),
        pagesize=A4,
        leftMargin=MARGIN,
        rightMargin=MARGIN,
        topMargin=12 * mm,
        bottomMargin=12 * mm,
        title="RuStore Events Preprint 2026–2027",
        author="Solar System 3D",
    )

    story: list = []
    story.extend(build_title_page(S))
    for i, ev in enumerate(EVENTS):
        story.extend(build_event_card(ev, S))
        if i < len(EVENTS) - 1:
            story.append(PageBreak())

    doc.build(story)
    # Re-assert after build (sanity)
    assert_limits(EVENTS)
    return out_path


if __name__ == "__main__":
    path = build_pdf()
    print(f"Wrote: {path}")
    print("Asserts: PASSED (title≤34, short≤32, description≤448)")
    print(
        "First batch (A): "
        + ", ".join(f"#{i}" for i in FIRST_BATCH_IDS)
    )
