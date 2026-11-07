"""
Turns the raw ChatGPT images into the exact game assets (names, transparency, crops, sizes).
Run from anywhere; paths are absolute. Originals are kept in ArtSource/ChatGPT/.
"""
import os, json, shutil, glob
from PIL import Image, ImageDraw, ImageFilter, ImageChops

PROJ = r'J:\Unity Projects HDD\Water Sort'
ART = os.path.join(PROJ, 'Assets', 'Resources', 'Art')
STORE = os.path.join(PROJ, 'Assets', 'Art', 'Store')
SRC = os.path.join(PROJ, 'ArtSource', 'ChatGPT')
os.makedirs(STORE, exist_ok=True); os.makedirs(SRC, exist_ok=True)

def find(sub):
    hits = [f for f in os.listdir(SRC) if sub.lower() in f.lower() and f.lower().endswith('.png')]
    assert hits, 'missing source: ' + sub
    return os.path.join(SRC, sorted(hits)[0])

def opn(sub): return Image.open(find(sub)).convert('RGBA')

# ---------------------------------------------------------------- helpers
def flood_outside(mask_L, seeds_border=True):
    """mask_L: 'L' image, 255 = candidate background. Returns image where background connected to the border is 255, everything else 0."""
    w, h = mask_L.size
    work = mask_L.copy()
    marker = 128
    for x in range(w):
        for y in (0, h - 1):
            if work.getpixel((x, y)) == 255: ImageDraw.floodfill(work, (x, y), marker)
    for y in range(h):
        for x in (0, w - 1):
            if work.getpixel((x, y)) == 255: ImageDraw.floodfill(work, (x, y), marker)
    return work.point(lambda v: 255 if v == marker else 0)

def key_light_background(im, min_ch=225, sat=22):
    """Removes a light neutral background (white, or a faint grey checkerboard that was baked into the picture) that touches the border."""
    rgb = im.convert('RGB')
    r, g, b = rgb.split()
    mn = ImageChops.darker(ImageChops.darker(r, g), b)
    mx = ImageChops.lighter(ImageChops.lighter(r, g), b)
    light = mn.point(lambda v: 255 if v >= min_ch else 0)
    neutral = ImageChops.subtract(mx, mn).point(lambda v: 255 if v <= sat else 0)
    cand = ImageChops.multiply(light, neutral)
    bg = flood_outside(cand)
    alpha = bg.point(lambda v: 0 if v else 255)
    alpha = alpha.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))     # eat the white fringe, soften the edge
    out = im.copy(); out.putalpha(alpha)
    return out

def trim(im, pad=0):
    bb = im.getchannel('A').point(lambda v: 255 if v > 8 else 0).getbbox()
    if bb is None: return im
    bb = (max(0, bb[0] - pad), max(0, bb[1] - pad), min(im.width, bb[2] + pad), min(im.height, bb[3] + pad))
    return im.crop(bb)

def square(im, size, fill=0.86):
    im = trim(im); s = max(im.size)
    canvas = Image.new('RGBA', (s, s), (0, 0, 0, 0)); canvas.paste(im, ((s - im.width) // 2, (s - im.height) // 2))
    inner = int(size * fill); canvas = canvas.resize((inner, inner), Image.LANCZOS)
    out = Image.new('RGBA', (size, size), (0, 0, 0, 0)); out.paste(canvas, ((size - inner) // 2, (size - inner) // 2))
    return out

def save(im, folder, name):
    path = os.path.join(folder, name); im.save(path, optimize=True); print('wrote', os.path.relpath(path, PROJ), im.size)

# ---------------------------------------------------------------- 0. archive the originals (outside Assets: not built, not imported)
for f in list(glob.glob(os.path.join(ART, '*.png'))):
    base = os.path.basename(f)
    if base.startswith('Bold Purple Geometric'): new = 'symbol_grid.png'
    elif base.startswith('Glossy Cartoon Game Icon Grid'): new = 'icon_grid.png'
    elif base.endswith('(1).png'): new = None            # duplicate splash
    else: new = base
    if new is None: os.remove(f); continue
    shutil.move(f, os.path.join(SRC, new))
for m in glob.glob(os.path.join(ART, '*.meta')): os.remove(m)

# ---------------------------------------------------------------- 1. the pot set (all cropped to ONE shared frame)
pot_raw = opn('Transparent Paint Mixing Cup')
W, H = pot_raw.size
alpha = pot_raw.getchannel('A')
bb = alpha.point(lambda v: 255 if v > 40 else 0).getbbox()
M = 20
crop = (bb[0] - M, bb[1] - M, bb[2] + M, bb[3] + M)
CW, CH = crop[2] - crop[0], crop[3] - crop[1]
OUT_W = 600; OUT_H = round(OUT_W * CH / CW)
def cropped(im): return im.crop(crop).resize((OUT_W, OUT_H), Image.LANCZOS)

pot = cropped(pot_raw)
save(pot, ART, 'pot.png')

# interior silhouette from the pot's OWN outline: everything not connected to the outside is "pot"
a_bin = alpha.point(lambda v: 0 if v > 40 else 255)
solid = flood_outside(a_bin).point(lambda v: 0 if v else 255)                  # 255 inside the outline (and on the outline)
solid = solid.crop(crop)
RIM_CUT = 452 - crop[1]                                                         # below the opaque rim band; the rim hides anything above
mask_hi = Image.new('L', solid.size, 0); mask_hi.paste(solid.crop((0, RIM_CUT, solid.width, solid.height)), (0, RIM_CUT))
mask_hi = mask_hi.filter(ImageFilter.MinFilter(9)).filter(ImageFilter.GaussianBlur(1.2))   # erode a few px so paint never pokes past the outline
mask = Image.new('RGBA', mask_hi.size, (255, 255, 255, 0)); mask.putalpha(mask_hi)
save(mask.resize((OUT_W, OUT_H), Image.LANCZOS), ART, 'pot_mask.png')
back = Image.new('RGBA', mask_hi.size, (255, 255, 255, 0)); back.putalpha(mask_hi.point(lambda v: int(v * 0.42)))
save(back.resize((OUT_W, OUT_H), Image.LANCZOS), ART, 'pot_back.png')

# cover: remove the faux checkerboard, then fit its silhouette onto the pot's own bounding box
cov = trim(key_light_background(opn('Burlap-Covered Pot')))
pb = (bb[0] - crop[0], bb[1] - crop[1], bb[2] - crop[0], bb[3] - crop[1])      # pot bbox inside the crop
cov = cov.resize((pb[2] - pb[0], pb[3] - pb[1]), Image.LANCZOS)
frame = Image.new('RGBA', (CW, CH), (0, 0, 0, 0)); frame.paste(cov, (pb[0], pb[1]), cov)
save(frame.resize((OUT_W, OUT_H), Image.LANCZOS), ART, 'pot_cover.png')

sh = trim(opn('Soft Purple Oval Shadow'), 2)
save(sh.resize((512, round(512 * sh.height / sh.width)), Image.LANCZOS), ART, 'pot_shadow.png')

# geometry for the game (measured in the 1024x1536 source, converted to frame units where the frame is PotW x PotH)
PW = 280.0; PH = PW * CH / CW; k = PW / CW                                      # frame units per source pixel
def up(y_src): return (CH - (y_src - crop[1])) * k                              # frame y measured from the bottom edge
sil = solid.crop((0, 0, solid.width, solid.height))
def half_width_at(y_src):
    row = solid.crop((0, y_src - crop[1], solid.width, y_src - crop[1] + 1)).getbbox()
    return ((row[2] - row[0]) / 2) * k if row else 0
ears_y = bb[1] + 0.07 * (bb[3] - bb[1])                                         # widest rim region
geo = {
    'PotW': round(PW, 1), 'PotH': round(PH, 1),
    'InnerBottom': round(up(1290), 1),                                          # where paint starts (above the thick glass base)
    'FillHeight': round(up(458) - up(1290), 1),                                 # 4 layers reach just under the rim
    'RimTop': round(up(bb[1]), 1), 'EarY': round(up(bb[1] + 0.085 * (bb[3] - bb[1])), 1),
    'RimHalfWidth': round((bb[2] - bb[0]) / 2 * k, 1),
    'BottomHalfWidth': round(half_width_at(1280), 1), 'TopHalfWidth': round(half_width_at(470), 1),
    'BottomY': round(up(1290), 1), 'TopY': round(up(458), 1),
    'BodyBottomY': round(up(bb[3]), 1),
}
json.dump(geo, open(os.path.join(os.path.dirname(__file__), 'pot_geometry.json'), 'w'), indent=1)
print(json.dumps(geo))

# ---------------------------------------------------------------- 2. UI shapes
pill = trim(opn('White Pill Button'), 2); save(pill.resize((512, 256), Image.LANCZOS), ART, 'round.png')
glow = trim(opn('Soft White Rounded Glow'), 2); save(glow.resize((512, 256), Image.LANCZOS), ART, 'roundsoft.png')

# ---------------------------------------------------------------- 3. effects
save(square(opn('White Bubble'), 256, 0.96), ART, 'circle.png')
save(square(opn('Four-Point Sparkle'), 256, 0.96), ART, 'sparkle.png')
save(square(opn('Organic Paint Splatter'), 256, 0.96), ART, 'splat.png')

# ---------------------------------------------------------------- 4. icons (4x4 grid) and symbols (4x3 grid)
def slice_grid(sub, cols, rows, names, size, fill, min_ch=232):
    im = Image.open(find(sub)).convert('RGBA'); w, h = im.size
    cw, ch = w / cols, h / rows
    for i, name in enumerate(names):
        if name is None: continue
        c, r = i % cols, i // cols
        cell = im.crop((int(c * cw), int(r * ch), int((c + 1) * cw), int((r + 1) * ch)))
        cell = key_light_background(cell, min_ch=min_ch, sat=26)
        save(square(cell, size, fill), ART, name + '.png')

slice_grid('icon_grid', 4, 4, ['coin', 'heart', 'star', 'check', 'plus', 'lock', 'gear', 'back', 'undo', 'shuffle', 'restart', 'trophy', 'gift', 'medal', 'bag', None], 256, 0.9)
slice_grid('symbol_grid', 4, 3, ['symbol_%d' % n for n in range(11)] + [None], 192, 0.82)

# ---------------------------------------------------------------- 5. brand and backdrops
logo = trim(opn('PAINT MAGIC SORT Logo'), 4); save(logo.resize((1000, round(1000 * logo.height / logo.width)), Image.LANCZOS), ART, 'logo.png')
bd = Image.open(find('Pastel Art Studio Shelf')).convert('RGB'); save(bd, ART, 'backdrop.png')
bw = Image.open(find('studio_landscape')).convert('RGB'); save(bw, ART, 'backdrop_wide.png')

# ---------------------------------------------------------------- 6. store art (outside Resources so it is not built into the game)
jar = trim(opn('Rainbow Paint Jar Sticker'), 0)
def gradient(size):
    g = Image.new('RGB', (1, size));
    for y in range(size):
        t = y / (size - 1); g.putpixel((0, y), (int(255 - 8 * t), int(236 - 18 * t), int(214 - 14 * t)))
    return g.resize((size, size))
icon = gradient(1024).convert('RGBA'); j = jar.copy(); j.thumbnail((760, 760)); icon.paste(j, ((1024 - j.width) // 2, (1024 - j.height) // 2 + 10), j)
save(icon.convert('RGB'), STORE, 'app_icon.png')
fg = Image.new('RGBA', (432, 432), (0, 0, 0, 0)); j2 = jar.copy(); j2.thumbnail((250, 250)); fg.paste(j2, ((432 - j2.width) // 2, (432 - j2.height) // 2), j2)
save(fg, STORE, 'icon_foreground.png'); save(gradient(432).convert('RGB'), STORE, 'icon_background.png')
shutil.copy(find('banner_320x180'), os.path.join(STORE, 'tv_banner.png'))
shutil.copy(find('store_banner_1024x500'), os.path.join(STORE, 'feature_graphic.png'))
shutil.copy(find('splash_screen_1080x1920'), os.path.join(STORE, 'splash.png'))
print('done')
