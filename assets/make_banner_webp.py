"""Generate an animated WebP from banner.png with a slow, dignified seesaw
(Wippe) motion — the ends tilt up and down around the centre pivot."""
from PIL import Image
import math

SRC = "banner.png"
OUT = "banner.webp"

FRAMES = 72           # total frames in the loop
DURATION_MS = 130     # per-frame duration — slow, dignified seesaw (~9.4 s loop)
MAX_ANGLE = 1.7       # max tilt in degrees (the seesaw ends go up/down)

orig = Image.open(SRC).convert("RGBA")
OW, OH = orig.size

# Transparent fill so the banner keeps its transparency on any background.
bg = (0, 0, 0, 0)

# Pad vertically so the lifted ends are not clipped during the tilt.
PAD = 22
base = Image.new("RGBA", (OW, OH + 2 * PAD), bg)
base.paste(orig, (0, PAD), orig)
W, H = base.size


frames = []
for i in range(FRAMES):
    t = math.sin(2 * math.pi * i / FRAMES)        # -1 .. 1 smooth loop
    angle = MAX_ANGLE * t                          # seesaw tilt around the centre

    # Pure rotation around the centre: one end rises while the other lowers,
    # like a seesaw (Wippe). No perspective — keeps it clean and serious.
    frame = base.rotate(angle, resample=Image.BICUBIC, expand=False, fillcolor=bg)

    frames.append(frame)

frames[0].save(
    OUT,
    save_all=True,
    append_images=frames[1:],
    duration=DURATION_MS,
    loop=0,
    quality=85,
    method=6,
    lossless=False,
    allow_mixed=True,
)
print(f"wrote {OUT} ({len(frames)} frames, {W}x{H})")
