"""Bakes one continuous floor using native-size samples of the original painting.

Repositions street strips, clears the weapons courtyard and extends the dry moat.
Development only: Pillow and NumPy. No source image upscaling; deterministic seed.
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
WIDTH, HEIGHT = 2305, 2975
PATCH, OVERLAP = 72, 18
STRIDE = PATCH - OVERLAP
SEED = 1201026
CORE_WIDTH, CORE_HEIGHT = 965, 1629
PIXELS_X, PIXELS_Y = CORE_WIDTH / 26, CORE_HEIGHT / 44


def minimum_cut(error):
    """Lowest-error path down an overlapping strip; leaves original pixels sharp."""
    total = error[0].copy()
    parents = np.zeros(error.shape, np.int16)
    for row in range(1, error.shape[0]):
        choices = np.stack((np.r_[np.inf, total[:-1]], total, np.r_[total[1:], np.inf]))
        step = choices.argmin(axis=0)
        parents[row] = np.arange(error.shape[1]) + step - 1
        total = error[row] + choices[step, np.arange(error.shape[1])]
    path = np.empty(error.shape[0], np.int16)
    path[-1] = total.argmin()
    for row in range(error.shape[0] - 1, 0, -1):
        path[row - 1] = parents[row, path[row]]
    return path


def candidates(core, rng):
    fields = []
    # Natural ground at the original corners: excludes foundations, grates and paving.
    regions = ((0, 0, 176, 190), (790, 0, 175, 190), (0, 1550, 210, 79), (755, 1550, 210, 79))
    for x, y, w, h in regions:
        for _ in range(56):
            px = x + rng.integers(0, w - PATCH + 1)
            py = y + rng.integers(0, h - PATCH + 1)
            tile = core[py:py + PATCH, px:px + PATCH].copy()
            tile = np.rot90(tile, int(rng.integers(4)))
            if rng.random() < .5:
                tile = tile[:, ::-1]
            fields.append(tile.copy())
    roads = []
    # Grates belong to the authored streets, not to the texture-synthesis palette.
    grates = ((492, 468, 545, 512), (484, 686, 537, 736), (416, 977, 476, 1037), (480, 1348, 535, 1404))
    red, green, blue = core.astype(np.float32).transpose(2, 0, 1)
    warm = (red > 120) & (green > 85) & (red > green * 1.18) & (red > blue * 1.65)
    for y in range(400, core.shape[0] - PATCH, 18):
        for x in range(300, min(690, core.shape[1] - PATCH), 18):
            contains_grate = any(x < rx1 and x + PATCH > rx0 and y < ry1 and y + PATCH > ry0
                                 for rx0, ry0, rx1, ry1 in grates)
            if not contains_grate and warm[y:y + PATCH, x:x + PATCH].mean() >= .62:
                tile = core[y:y + PATCH, x:x + PATCH].copy()
                roads.append(tile)
                roads.append(tile[:, ::-1].copy())
    if not roads:
        raise ValueError('No native road samples found.')
    # Locate the actual road where it leaves the source image, rather than imposing a new alignment.
    columns = warm[-48:].mean(axis=0) > .48
    indices = np.flatnonzero(columns)
    groups = np.split(indices, np.where(np.diff(indices) > 1)[0] + 1)
    groups = [group for group in groups if len(group) >= 24]
    road = min(groups, key=lambda group: abs(group.mean() - core.shape[1] / 2))
    return np.asarray(fields, dtype=np.float32), np.asarray(roads, dtype=np.float32), float(road.mean()), len(road)


def quilt(width, height, rng, pool_at, progress=False):
    canvas = np.zeros((height, width, 3), np.uint8)
    for y in range(0, height, STRIDE):
        for x in range(0, width, STRIDE):
            hh, ww = min(PATCH, height - y), min(PATCH, width - x)
            old = canvas[y:y + hh, x:x + ww].astype(np.float32)
            known = np.zeros((hh, ww), bool)
            if x:
                known[:, :min(OVERLAP, ww)] = True
            if y:
                known[:min(OVERLAP, hh), :] = True
            pool = pool_at(x + ww / 2, y + hh / 2)
            samples = pool[rng.integers(0, len(pool), size=28), :hh, :ww]
            if known.any():
                errors = ((samples[:, known] - old[known]) ** 2).mean(axis=(1, 2))
                best = errors.min()
                eligible = np.flatnonzero(errors <= best * 1.08 + 1)
                new = samples[rng.choice(eligible)]
            else:
                new = samples[0]
            keep = np.zeros((hh, ww), bool)
            error = ((new - old) ** 2).sum(axis=2)
            if x:
                cut = minimum_cut(error[:, :min(OVERLAP, ww)])
                keep |= np.arange(ww)[None, :] < cut[:, None]
            if y:
                cut = minimum_cut(error[:min(OVERLAP, hh), :].T)
                keep |= np.arange(hh)[:, None] < cut[None, :]
            canvas[y:y + hh, x:x + ww] = np.where(keep[..., None], old, new).astype(np.uint8)
        if progress and y % (STRIDE * 8) == 0:
            print(f'Quilting: {min(100, round((y + PATCH) / height * 100))}%', flush=True)
    return canvas


def blend_region(canvas, patch, x, y, feather=12):
    """Short edge transition; the interior retains native grain and pixel size."""
    h, w = patch.shape[:2]
    yy, xx = np.mgrid[:h, :w]
    edge = np.minimum.reduce((xx, yy, w - 1 - xx, h - 1 - yy))
    alpha = np.clip(edge / feather, 0, 1)[..., None]
    old = canvas[y:y + h, x:x + w].astype(np.float32)
    canvas[y:y + h, x:x + w] = (old * (1 - alpha) + patch * alpha).astype(np.uint8)


def native_pool(core, regions, rng):
    tiles = []
    for x, y, w, h in regions:
        for _ in range(80):
            px = x + rng.integers(w - PATCH + 1)
            py = y + rng.integers(h - PATCH + 1)
            tile = np.rot90(core[py:py + PATCH, px:px + PATCH], int(rng.integers(4)))
            tiles.append(tile.copy())
    return np.asarray(tiles, dtype=np.float32)


def rebuild_streets(source, fields, roads, rng):
    core = source.copy()
    # Move both built-up strips one world unit outwards. Native pixels are translated,
    # not stretched; new road samples fill the additional space on either side.
    shift, start, left_cut, right_cut = round(PIXELS_X), round((22 - 10.1) * PIXELS_Y), 415, 550
    core[start:, :left_cut - shift] = source[start:, shift:left_cut]
    core[start:, right_cut + shift:] = source[start:, right_cut:-shift]
    for x in (left_cut - shift - 12, right_cut - 12):
        fill = quilt(shift + 24, CORE_HEIGHT - start, rng, lambda x, y: roads)
        blend_region(core, fill, x, start)
    # The baker's former plot becomes the open approach to the weapons shop.
    # Broader than the old foundation, so its moss border does not remain on the floor.
    def repaint(rect, pool):
        x0, y0, x1, y1 = rect
        blend_region(core, quilt(x1 - x0, y1 - y0, rng, lambda x, y: pool), x0, y0, 18)
    repaint((594, 1290, 789, 1530), roads)
    # Eastern outer row has three rearranged houses; clear their obsolete footprints.
    repaint((788, 1103, 949, 1616), fields)
    return core


def bake(source):
    rng = np.random.default_rng(SEED)
    field_tiles, road_tiles, road_x, road_width = candidates(source, rng)
    core = rebuild_streets(source, field_tiles, road_tiles, rng)
    h, w = core.shape[:2]
    left, top = (WIDTH - w) // 2, (HEIGHT - h) // 2
    def pool_at(x, y):
        curve = np.sin(max(0, y - top - h) / 210) * 9
        in_road = y > top + h - PATCH and abs(x - left - road_x - curve) < road_width * .62
        return road_tiles if in_road else field_tiles
    canvas = quilt(WIDTH, HEIGHT, rng, pool_at, progress=True)

    # Reflect the original edge grain into a short exterior band. Low-frequency colour
    # matching below preserves the fine native texture instead of blurring it.
    yy, xx = np.mgrid[:HEIGHT, :WIDTH]
    dx = np.maximum(left - xx, np.maximum(xx - (left + w - 1), 0))
    dy = np.maximum(top - yy, np.maximum(yy - (top + h - 1), 0))
    distance = np.maximum(dx, dy).astype(np.float32)
    sx = np.where(xx < left, left - xx - 1, np.where(xx >= left + w, 2 * w + left - xx - 1, xx - left))
    sy = np.where(yy < top, top - yy - 1, np.where(yy >= top + h, 2 * h + top - yy - 1, yy - top))
    reflected = core[np.clip(sy, 0, h - 1), np.clip(sx, 0, w - 1)]
    old_low = np.asarray(Image.fromarray(canvas).filter(ImageFilter.GaussianBlur(7)), dtype=np.float32)
    edge_low = np.asarray(Image.fromarray(reflected).filter(ImageFilter.GaussianBlur(7)), dtype=np.float32)
    band = np.clip(1 - distance / 96, 0, 1)[..., None]
    matched = np.clip(canvas.astype(np.float32) + (edge_low - old_low) * band, 0, 255)
    reflected_band = np.clip(1 - distance / 32, 0, 1)[..., None]
    canvas = np.clip(matched * (1 - reflected_band) + reflected * reflected_band, 0, 255).astype(np.uint8)
    # Rebuilt playable core stays at its original texel density, without resizing.
    canvas[top:top + h, left:left + w] = core
    # The painted rocky bed now follows the full-width depressed mesh (10.1..14.9).
    pit_top = top + round((22 - 14.9) * PIXELS_Y)
    pit_bottom = top + round((22 - 10.1) * PIXELS_Y)
    rock_tiles = native_pool(source, ((0, 252, 350, 150), (620, 252, 345, 150)), rng)
    canvas[pit_top:pit_bottom] = quilt(WIDTH, pit_bottom - pit_top, rng, lambda x, y: rock_tiles)
    # The bridge is stone all the way to its southern approach (9.6..15.4).
    bx0, bx1 = left + round((13 - 1.85) * PIXELS_X), left + round((13 + 1.85) * PIXELS_X)
    by0, by1 = top + round((22 - 15.4) * PIXELS_Y), top + round((22 - 9.6) * PIXELS_Y)
    paving = native_pool(source, ((428, 252, 112, 168),), rng)
    canvas[by0:by1, bx0:bx1] = quilt(bx1 - bx0, by1 - by0, rng, lambda x, y: paving)
    return canvas, {'x': left, 'y': top, 'width': w, 'height': h}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--core-output', type=Path, help='Optional rebuilt core reference for pixel inspection.')
    args = parser.parse_args()
    source = ROOT / 'Docs/ArtInputs/CityGroundCore.png'
    core = np.asarray(Image.open(source).convert('RGB'))
    if core.shape[:2] != (1629, 965):
        raise ValueError('Unexpected source layout; camera mapping must be updated with source dimensions.')
    canvas, locked = bake(core)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(canvas).save(args.output)
    reread = np.asarray(Image.open(args.output).convert('RGB'))
    if not np.array_equal(reread, canvas):
        raise ValueError('Saved PNG changed the baked pixels.')
    rebuilt = canvas[locked['y']:locked['y'] + locked['height'], locked['x']:locked['x'] + locked['width']]
    if args.core_output:
        args.core_output.parent.mkdir(parents=True, exist_ok=True)
        Image.fromarray(rebuilt).save(args.core_output)
    metadata = {'width': WIDTH, 'height': HEIGHT, 'core': locked, 'seed': SEED,
                'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                'sha256': hashlib.sha256(args.output.read_bytes()).hexdigest(), 'resizedCore': False,
                'corePixelSha256': hashlib.sha256(rebuilt.tobytes()).hexdigest(),
                'layout': 'wider-streets-open-weapons-courtyard-full-width-moat'}
    args.output.with_suffix('.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print(json.dumps(metadata), flush=True)


if __name__ == '__main__':
    main()
