"""Generate the README banner (logo + wordmark) from the package logo.

Outputs a transparent PNG that renders on both GitHub light and dark themes.
Run:  python scripts/generate-banner.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
LOGO = ROOT / "src" / "OpenTelemetryExtension.Configuration" / "logo.png"
OUT = ROOT / "assets" / "banner.png"

TEXT = "OpenTelemetryExtension.Configuration"
TEXT_COLOR = (47, 129, 214, 255)   # #2F81D6 — readable on light & dark
SCALE = 2                          # supersample for crisp text
LOGO_H = 72
GAP = 22
PAD_X = 8
PAD_Y = 10

# Segoe UI Semibold, with graceful fallbacks.
FONT_CANDIDATES = [
    r"C:\Windows\Fonts\seguisb.ttf",
    r"C:\Windows\Fonts\segoeui.ttf",
    r"C:\Windows\Fonts\arialbd.ttf",
]
def load_font(size):
    for path in FONT_CANDIDATES:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()

s = SCALE
logo = Image.open(LOGO).convert("RGBA")
lh = LOGO_H * s
lw = round(logo.width * lh / logo.height)
logo = logo.resize((lw, lh), Image.LANCZOS)

font = load_font(round(40 * s))
tmp = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
bbox = tmp.textbbox((0, 0), TEXT, font=font)
tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]

W = PAD_X * s + lw + GAP * s + tw + PAD_X * s
H = PAD_Y * s * 2 + max(lh, th)

canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
draw = ImageDraw.Draw(canvas)

# Vertically center both elements.
canvas.alpha_composite(logo, (PAD_X * s, (H - lh) // 2))
text_x = PAD_X * s + lw + GAP * s
draw.text((text_x - bbox[0], (H - th) // 2 - bbox[1]), TEXT, font=font, fill=TEXT_COLOR)

canvas = canvas.resize((W // s, H // s), Image.LANCZOS)
OUT.parent.mkdir(parents=True, exist_ok=True)
canvas.save(OUT)
print(f"wrote {OUT} ({canvas.width}x{canvas.height})")
