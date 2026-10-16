"""Registers the generated dusk painting to the final Unity city layout.

The painting supplies all terrain and baked light. This importer only registers
its landmarks and shop light pools, then samples with nearest-neighbour pixels.
Development dependency: Pillow/NumPy. Runtime uses the resulting PNG only.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
WIDTH, HEIGHT = 2305, 2975
CORE_WIDTH, CORE_HEIGHT = 965, 1629
PIXELS_X, PIXELS_Y = CORE_WIDTH / 26, CORE_HEIGHT / 44
# Authored pool centres in the image-generation source, in native source pixels.
# Their destinations come from each actual facade, not obsolete painted lots.
SOURCE_SIZE = (1104, 1425)
LIGHTS = {
    'Loja de materias': (665, 716),
    'Loja de itens': (423, 835),
    'Loja de equipamentos': (650, 943),
    'Hospedaria': (432, 1035),
}


def doors(layout):
    text = layout.read_text(encoding='utf-8-sig')
    result = {}
    for name in LIGHTS:
        match = re.search(r'new CityBlock\("' + re.escape(name)
                          + r'",\s*([-\d.]+)f?,\s*([-\d.]+)f?,\s*([-\d.]+)f?,\s*([-\d.]+)f?', text)
        if not match:
            raise ValueError('Missing building: ' + name)
        x, y, width, height = map(float, match.groups())
        result[name] = (x, y - height / 2 - .035)
    return result


def bake(source, layout):
    if source.size != SOURCE_SIZE:
        raise ValueError(f'Painting registration expects {SOURCE_SIZE}, got {source.size}.')
    sw, sh = source.size
    yy, xx = np.mgrid[:HEIGHT, :WIDTH].astype(np.float32)
    native_x = (xx + .5) * sw / WIDTH - .5
    native_y = (yy + .5) * sh / HEIGHT - .5
    # The generated moat is registered to the two physical lips (14.9 and 10.1).
    # This also retains the single bridge's position and the full exterior band.
    target_y = [0, HEIGHT / 2 - 14.9 * PIXELS_Y,
                HEIGHT / 2 - 10.1 * PIXELS_Y, HEIGHT - 1]
    source_y = [0, 435, 538, sh - 1]
    sample_y = np.interp(yy, target_y, source_y).astype(np.float32)
    anchors = []
    for name, (x, door_y) in doors(layout).items():
        # Centre the pool just in front of the door; the rest spreads into its apron.
        tx = WIDTH / 2 + x * PIXELS_X
        ty = HEIGHT / 2 - (door_y - .9) * PIXELS_Y
        sx, sy = LIGHTS[name]
        anchors.append({'building': name, 'door': [x, door_y],
                        'lightCentre': [tx, ty], 'sourceCentre': [sx, sy]})
    by_name = {anchor['building']: anchor for anchor in anchors}
    def native_centre(name):
        x, y = by_name[name]['lightCentre']
        return (x + .5) * sw / WIDTH - .5, (y + .5) * sh / HEIGHT - .5
    lx, ly = native_centre('Loja de itens')
    nx, ny = native_centre('Hospedaria')
    rx, ry = native_centre('Loja de materias')
    wx, wy = native_centre('Loja de equipamentos')
    # Monotonic, broad strip registration preserves the terrain grain: no local
    # radial displacement that would stretch fine pixels around a light pool.
    left_target = np.interp(native_y, [0, ly, ny, sh - 1], [lx, lx, nx, nx])
    left_source = np.interp(native_y, [0, ly, ny, sh - 1], [423, 423, 432, 432])
    right_target = np.interp(native_y, [0, ry, wy, sh - 1], [rx, rx, wx, wx])
    right_source = np.interp(native_y, [0, ry, wy, sh - 1], [665, 665, 650, 650])
    mapped_x = np.empty_like(native_x)
    for row in range(HEIGHT):
        mapped_x[row] = np.interp(native_x[row],
            [0, 270, left_target[row, 0], sw / 2, right_target[row, 0], 840, sw - 1],
            [0, 270, left_source[row, 0], sw / 2, right_source[row, 0], 840, sw - 1])
    village = np.clip((native_y - 550) / 80, 0, 1)
    sample_x = native_x + (mapped_x - native_x) * village
    left_y = np.interp(native_y, [0, 620, ly, ny, 1160, sh - 1], [0, 620, 835, 1035, 1160, sh - 1])
    right_y = np.interp(native_y, [0, 620, ry, wy, 1100, sh - 1], [0, 620, 716, 943, 1100, sh - 1])
    left_weight = np.clip(1 - abs(native_x - left_target) / 100, 0, 1) * village
    right_weight = np.clip(1 - abs(native_x - right_target) / 100, 0, 1) * village
    sample_y += (left_y - sample_y) * left_weight + (right_y - sample_y) * right_weight
    ix = np.clip(np.rint(sample_x).astype(np.int32), 0, sw - 1)
    iy = np.clip(np.rint(sample_y).astype(np.int32), 0, sh - 1)
    canvas = np.asarray(source.convert('RGB'))[iy, ix]
    return canvas, anchors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, default=ROOT / 'Docs/ArtInputs/CityGroundNatural.png')
    parser.add_argument('--layout', type=Path, default=ROOT / 'Assets/Scripts/Prototype/PrototypeCityLayout.cs')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--core-output', type=Path)
    parser.add_argument('--manifest', type=Path, default=ROOT / 'Docs/ArtInputs/CityGroundBake.json')
    args = parser.parse_args()
    source = Image.open(args.source)
    canvas, anchors = bake(source, args.layout)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(canvas).save(args.output)
    if not np.array_equal(np.asarray(Image.open(args.output)), canvas):
        raise ValueError('Saved PNG changed the registered pixels.')
    left, top = (WIDTH - CORE_WIDTH) // 2, (HEIGHT - CORE_HEIGHT) // 2
    core = canvas[top:top + CORE_HEIGHT, left:left + CORE_WIDTH]
    if args.core_output:
        args.core_output.parent.mkdir(parents=True, exist_ok=True)
        Image.fromarray(core).save(args.core_output)
    metadata = {'width': WIDTH, 'height': HEIGHT,
                'core': {'x': left, 'y': top, 'width': CORE_WIDTH, 'height': CORE_HEIGHT},
                'source': 'Docs/ArtInputs/CityGroundNatural.png', 'sourceSize': list(source.size),
                'sourceSha256': hashlib.sha256(args.source.read_bytes()).hexdigest(),
                'sha256': hashlib.sha256(args.output.read_bytes()).hexdigest(),
                'sampling': 'nearest-neighbour with continuous landmark registration',
                'corePixelSha256': hashlib.sha256(core.tobytes()).hexdigest(),
                'layout': 'organic-dusk-paths-final-building-positions-baked-shop-lights',
                'lights': anchors}
    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.write_text(json.dumps(metadata, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(json.dumps(metadata, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
