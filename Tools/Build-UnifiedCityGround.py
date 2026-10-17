"""Installs the native-resolution city painting without rescaling or warping it.

All artistic edits are baked in CityGroundNatural.png. The original normalized
map coordinates remain the calibration for existing meshes, buildings and UVs.
"""
import argparse
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
NATIVE_SIZE = (2688, 3472)
ORIGINAL_SIZE = (2305, 2975)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, default=ROOT / 'Docs/ArtInputs/CityGroundNatural.png')
    parser.add_argument('--generation', type=Path, default=ROOT / 'Docs/ArtInputs/CityGroundNativeGeneration.json')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--core-output', type=Path)
    parser.add_argument('--manifest', type=Path, default=ROOT / 'Docs/ArtInputs/CityGroundBake.json')
    args = parser.parse_args()
    image = Image.open(args.source)
    image.load()
    if image.size != NATIVE_SIZE or image.mode != 'RGB':
        raise ValueError(f'Expected opaque native RGB painting {NATIVE_SIZE}; got {image.size}, {image.mode}.')
    generation = json.loads(args.generation.read_text(encoding='utf-8'))
    if generation['upscaled'] or generation['assembledSha256'] != sha256(args.source):
        raise ValueError('Native generation manifest does not match the painting.')
    for tile in generation['sourceTiles']:
        if tile['upscaled'] or any(native < output for native, output in zip(tile['nativeSize'], tile['outputSlot'][2:])):
            raise ValueError('A generated source was enlarged: ' + tile['name'])
    # Copy the painting byte for byte. No interpolation, displacement or UV changes.
    args.output.parent.mkdir(parents=True, exist_ok=True)
    if args.source.resolve() != args.output.resolve():
        shutil.copyfile(args.source, args.output)
    if sha256(args.output) != sha256(args.source):
        raise ValueError('Output differs from the native source painting.')
    width, height = image.size
    left, top = round(670 / ORIGINAL_SIZE[0] * width), round(673 / ORIGINAL_SIZE[1] * height)
    right, bottom = round(1635 / ORIGINAL_SIZE[0] * width), round(2302 / ORIGINAL_SIZE[1] * height)
    if args.core_output:
        args.core_output.parent.mkdir(parents=True, exist_ok=True)
        image.crop((left, top, right, bottom)).save(args.core_output)
    metadata = dict(width=width, height=height,
        core=dict(x=left, y=top, width=right-left, height=bottom-top),
        source='Docs/ArtInputs/CityGroundNatural.png', sourceSize=list(image.size),
        sourceSha256=sha256(args.source), sha256=sha256(args.output), upscaled=False,
        sampling='byte-for-byte native source copy; no resampling or warp',
        coordinateCalibration=list(ORIGINAL_SIZE),
        layout='original-fixed-layout-native-detail-facade-matched-reflections',
        lights=generation['lights'], unchanged=generation['unchanged'])
    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.write_text(json.dumps(metadata,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps({'size':list(image.size),'upscaled':False,'sha256':sha256(args.output)}))


if __name__ == '__main__':
    main()
