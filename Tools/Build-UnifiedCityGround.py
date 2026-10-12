"""Bakes one floor PNG with native-size texture quilting; never resizes the original city.

Development only: Pillow and NumPy. Deterministic seed; game consumes the finished PNG.
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
    red, green, blue = core.astype(np.float32).transpose(2, 0, 1)
    warm = (red > 120) & (green > 85) & (red > green * 1.18) & (red > blue * 1.65)
    for y in range(400, core.shape[0] - PATCH, 18):
        for x in range(300, min(690, core.shape[1] - PATCH), 18):
            if warm[y:y + PATCH, x:x + PATCH].mean() >= .62:
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


def bake(core):
    rng = np.random.default_rng(SEED)
    field_tiles, road_tiles, road_x, road_width = candidates(core, rng)
    h, w = core.shape[:2]
    left, top = (WIDTH - w) // 2, (HEIGHT - h) // 2
    canvas = np.zeros((HEIGHT, WIDTH, 3), np.uint8)
    for y in range(0, HEIGHT, STRIDE):
        for x in range(0, WIDTH, STRIDE):
            hh, ww = min(PATCH, HEIGHT - y), min(PATCH, WIDTH - x)
            old = canvas[y:y + hh, x:x + ww].astype(np.float32)
            known = np.zeros((hh, ww), bool)
            if x:
                known[:, :min(OVERLAP, ww)] = True
            if y:
                known[:min(OVERLAP, hh), :] = True
            curve = np.sin(max(0, y - top - h) / 210) * 9
            in_road = y + hh / 2 > top + h - PATCH and abs(x + ww / 2 - left - road_x - curve) < road_width * .62
            pool = road_tiles if in_road else field_tiles
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
        if y % (STRIDE * 8) == 0:
            print(f'Quilting: {min(100, round((y + PATCH) / HEIGHT * 100))}%', flush=True)

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
    # Native pixels of the entire playable city are restored exactly, with no resizing.
    canvas[top:top + h, left:left + w] = core
    if not np.array_equal(canvas[top:top + h, left:left + w], core):
        raise ValueError('Core pixels were altered.')
    return canvas, {'x': left, 'y': top, 'width': w, 'height': h}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    source = ROOT / 'Docs/ArtInputs/CityGroundCore.png'
    core = np.asarray(Image.open(source).convert('RGB'))
    if core.shape[:2] != (1629, 965):
        raise ValueError('Unexpected source layout; camera mapping must be updated with source dimensions.')
    canvas, locked = bake(core)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(canvas).save(args.output)
    reread = np.asarray(Image.open(args.output).convert('RGB'))
    if not np.array_equal(reread[locked['y']:locked['y'] + locked['height'], locked['x']:locked['x'] + locked['width']], core):
        raise ValueError('Saved PNG changed the original core.')
    metadata = {'width': WIDTH, 'height': HEIGHT, 'core': locked, 'seed': SEED,
                'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                'sha256': hashlib.sha256(args.output.read_bytes()).hexdigest(), 'resizedCore': False}
    args.output.with_suffix('.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print(json.dumps(metadata), flush=True)


if __name__ == '__main__':
    main()
