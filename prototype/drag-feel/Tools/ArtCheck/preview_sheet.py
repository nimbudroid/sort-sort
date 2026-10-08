"""Before/after sheet from two ArtPreview output folders.

    python3 preview_sheet.py <beforeDir> <afterDir> <out.png> [--ids a,b,c] [--size 150] [--cols 7] [--res 160]

Objects are composited on the play-zone colour with the in-game drop shadow (offset down-right by 6%/8% of the
object size, like ToyShadow). If a folder has <id>.shadow.rgba (Molded) that soft sprite is used, otherwise a
tinted copy of the object (the previous shadow). Each cell: before on the left, after on the right.
~150 px per object is the on-phone size (ortho half-height 8 on a 2340 px tall screen = 146 px per world unit).
"""
import argparse, math, os
from PIL import Image, ImageDraw

ap = argparse.ArgumentParser()
ap.add_argument('before'); ap.add_argument('after'); ap.add_argument('out')
ap.add_argument('--ids'); ap.add_argument('--size', type=int, default=150); ap.add_argument('--cols', type=int, default=7)
ap.add_argument('--res', type=int, default=160); ap.add_argument('--labels', action='store_true')
a = ap.parse_args()
BG = (214, 236, 247); INK = (42, 35, 71); SHADOW = (41, 26, 77)

rows = [l.strip().split('|') for l in open(os.path.join(a.after, 'index.txt')) if l.strip()]
names = {r[0]: r for r in rows}
ids = a.ids.split(',') if a.ids else [r[0] for r in rows]

def load(d, oid, shadow=False):
    p = os.path.join(d, oid + ('.shadow.rgba' if shadow else '.rgba'))
    if not os.path.exists(p): return None
    raw = open(p, 'rb').read(); n = int(math.isqrt(len(raw) // 4))
    return Image.frombytes('RGBA', (n, n), raw).transpose(Image.FLIP_TOP_BOTTOM)

def draw(cell, d, oid, x, y, T):
    obj = load(d, oid)
    sh = load(d, oid, True)
    if sh is not None:
        span = 1.3  # 1 + 2 * ToyShading.ShadowPadding
        s = int(T * span); sh = sh.resize((s, s), Image.LANCZOS)
        alpha = sh.split()[3].point(lambda v: int(v * 0.3))
    else:
        s = T; sh = obj.resize((T, T), Image.LANCZOS); alpha = sh.split()[3].point(lambda v: int(v * 0.16))
    layer = Image.new('RGBA', sh.size, SHADOW + (0,)); layer.putalpha(alpha)
    cell.alpha_composite(layer, (int(x + T * 0.06 - (s - T) / 2), int(y + T * 0.08 - (s - T) / 2)))
    o = obj.resize((T, T), Image.LANCZOS)
    cell.alpha_composite(o, (int(x), int(y)))

T = a.size; pad = max(4, T // 12); lab = 16 if a.labels else 0
cw = 2 * T + 3 * pad; ch = T + 2 * pad + lab
cols = min(a.cols, len(ids)); R = (len(ids) + cols - 1) // cols
sheet = Image.new('RGBA', (cols * cw, R * ch), BG + (255,))
dr = ImageDraw.Draw(sheet)
for i, oid in enumerate(ids):
    cx, cy = (i % cols) * cw, (i // cols) * ch
    draw(sheet, a.before, oid, cx + pad, cy + pad, T)
    draw(sheet, a.after, oid, cx + 2 * pad + T, cy + pad, T)
    dr.line([(cx + cw - 1, cy), (cx + cw - 1, cy + ch)], fill=(190, 214, 228))
    if a.labels: dr.text((cx + pad, cy + T + pad), names[oid][1] + '  [' + names[oid][4] + ']', fill=INK)
sheet.convert('RGB').save(a.out); print(a.out, sheet.size)
