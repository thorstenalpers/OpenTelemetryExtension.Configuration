"""Generate an animated WebP from banner.png with a gentle 3D rocking (seesaw)
motion — the ends swing up and down around the centre."""
from PIL import Image
import math

SRC = "banner.png"
OUT = "banner.webp"

FRAMES = 48           # total frames in the loop
DURATION_MS = 80      # per-frame duration (~12.5 fps) — slow, gentle motion
MAX_ANGLE = 1.6       # max tilt in degrees (ends rocking)
MAX_PERSPECTIVE = 0.05  # subtle pseudo-3D depth on the swing

orig = Image.open(SRC).convert("RGBA")
OW, OH = orig.size

# Transparent fill so the banner keeps its transparency on any background.
bg = (0, 0, 0, 0)

# Pad vertically so the lifted ends are not clipped during the swing.
PAD = 22
base = Image.new("RGBA", (OW, OH + 2 * PAD), bg)
base.paste(orig, (0, PAD), orig)
W, H = base.size


def perspective_coeffs(src, dst):
    # solve for the 8 coefficients of a projective transform
    matrix = []
    for s, d in zip(src, dst):
        matrix.append([d[0], d[1], 1, 0, 0, 0, -s[0] * d[0], -s[0] * d[1]])
        matrix.append([0, 0, 0, d[0], d[1], 1, -s[1] * d[0], -s[1] * d[1]])
    A = matrix
    b = [c for s in src for c in s]
    # Gaussian elimination
    n = 8
    for i in range(n):
        piv = max(range(i, n), key=lambda r: abs(A[r][i]))
        A[i], A[piv] = A[piv], A[i]
        b[i], b[piv] = b[piv], b[i]
        for r in range(n):
            if r != i:
                f = A[r][i] / A[i][i]
                for c in range(i, n):
                    A[r][c] -= f * A[i][c]
                b[r] -= f * b[i]
    return [b[i] / A[i][i] for i in range(n)]


frames = []
for i in range(FRAMES):
    t = math.sin(2 * math.pi * i / FRAMES)        # -1 .. 1 smooth loop
    angle = MAX_ANGLE * t                          # rocking tilt
    p = MAX_PERSPECTIVE * t                        # depth on the swing

    # 1) gentle rotation around the centre (ends go up/down)
    frame = base.rotate(angle, resample=Image.BICUBIC, expand=False, fillcolor=bg)

    # 2) subtle perspective so the lifted end appears to come forward
    lift = p * H
    src = [(0, 0), (W, 0), (W, H), (0, H)]
    dst = [(0, -lift), (W, lift), (W, H - lift), (0, H + lift)]
    coeffs = perspective_coeffs(src, dst)
    frame = frame.transform((W, H), Image.PERSPECTIVE, coeffs,
                            resample=Image.BICUBIC, fillcolor=bg)

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
