"""Inspect source PNGs and write atlas coordinates; never changes image pixels or alpha."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

ACTORS = {"ArenOmnislash": 0, "ArenFinisher": 15, "LiaGospel": 15, "BramBastion": 15}
NAMES = (*ACTORS, "ArenImpact", "LiaCelestial", "BramShieldImpact", "BramBarrier")


def divisions(density, count=4):
    length = len(density)
    result = [0]
    for index in range(1, count):
        expected = length * index / count
        radius = length // 20
        candidates = np.arange(int(expected) - radius, int(expected) + radius + 1)
        score = density[candidates] + np.abs(candidates - expected) * .08
        result.append(int(candidates[np.argmin(score)]))
    return result + [length]


def describe(path):
    rgba = np.asarray(Image.open(path).convert("RGBA"))
    height, width = rgba.shape[:2]
    alpha = rgba[:, :, 3]
    assert (alpha == 0).mean() > .15, f"{path.name}: missing transparent alpha"
    mask = alpha >= 192
    rows = divisions(mask.sum(axis=1))
    frames, signatures, body_heights = [], [], []
    for row in range(4):
        top, bottom = rows[row:row+2]
        columns = divisions(mask[top:bottom].sum(axis=0))
        for col in range(4):
            left, right = columns[col:col+2]
            cell = rgba[top:bottom, left:right]
            solid = cell[:, :, 3] >= 192
            assert solid.sum() > 150, f"{path.name}: empty frame {row*4+col}"
            assert (cell[:, :, 3] == 0).mean() > .015, f"{path.name}: opaque frame matte"
            signatures.append(hashlib.sha256(cell.tobytes()).hexdigest())
            ch, cw = cell.shape[:2]
            pivot_x, pivot_y = cw * .5, ch * .85
            if path.stem in ACTORS:
                rgb = cell[:, :, :3].astype(float)
                dark = solid & (rgb.max(axis=2) < 220)
                warm = dark & (rgb[:, :, 0] > rgb[:, :, 1]*1.09+3) & (rgb[:, :, 1] > rgb[:, :, 2]*1.1)
                yy, xx = np.where(warm)
                assert len(yy) > 50, f"{path.name}: character body missing"
                # Ignore sparse aura/dust flecks. Brown boots determine a stable foot anchor.
                pivot_y = float(np.percentile(yy, 99.6)) + 1
                feet = (yy >= pivot_y - max(8, ch*.035)) & (yy <= pivot_y)
                pivot_x = float(np.median(xx[feet]))
                body_y, _ = np.where(dark)
                body_heights.append(pivot_y-float(np.percentile(body_y, .5)))
            frames.append(dict(x=left, y=top, width=right-left, height=bottom-top,
                               pivotX=round(pivot_x, 3), pivotY=round(pivot_y, 3)))
    assert len(set(signatures)) == 16, f"{path.name}: repeated frames"
    reference = body_heights[ACTORS[path.stem]] if path.stem in ACTORS else height/4
    assert reference > 20
    return dict(width=width, height=height, referenceHeight=round(reference, 3), frames=frames)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--write-metadata", action="store_true")
    args = parser.parse_args()
    folder = args.root / "Assets/Resources/PixelArt/Ultimates"
    for name in NAMES:
        path = folder / (name+".png")
        metadata = describe(path)
        output = folder / (name+"Frames.json")
        if args.write_metadata:
            output.write_text(json.dumps(metadata, indent=2)+"\n", encoding="utf-8")
        else:
            assert json.loads(output.read_text(encoding="utf-8")) == metadata, f"{name}: stale atlas coordinates"
        importer = folder / (name+".png.meta")
        if importer.exists():
            source = importer.read_text(encoding="utf-8-sig")
            for token in ("enableMipMap: 0", "filterMode: 0", "textureCompression: 0", "alphaIsTransparency: 1"):
                assert token in source, f"{name}: importer {token}"
        print(f"{name}: {metadata['width']}x{metadata['height']}, 16 distinct transparent frames, body scale {metadata['referenceHeight']}")
    print("128 unique sprite frames validated; native PNG pixels and alpha preserved.")


if __name__ == "__main__":
    main()
