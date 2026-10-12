"""Valida atlas e importadores; exporta metadados para previas sem modificar imagens."""
import json
import sys
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
directory = root / "Assets/Resources/PixelArt"
result = {}
checks = 0

def check(condition, message):
    global checks
    checks += 1
    if not condition:
        raise ValueError(message)

for name in ("ArenWalk", "LiaWalk", "BramWalk", "NpcRoster", "TerrainAtlas"):
    path = directory / (name + ".png")
    with Image.open(path) as image:
        width, height = image.size
        check(width % 4 == 0 and height % 3 == 0, name + ": grade irregular")
        check(width <= 2048 and height <= 2048, name + ": tamanho acima do importador")
        meta = path.with_suffix(".png.meta").read_text(encoding="utf-8-sig")
        for setting in ("filterMode: 0", "enableMipMap: 0", "nPOTScale: 0", "textureCompression: 0"):
            check(setting in meta, name + ": importacao incorreta " + setting)
        entry = {"width": width, "height": height, "frames": []}
        if name != "TerrainAtlas":
            check(image.mode == "RGBA", name + ": canal alpha ausente")
            check("isReadable: 1" in meta, name + ": alpha precisa ser legivel em runtime")
            alpha = image.getchannel("A")
            check(alpha.getextrema() == (0, 255), name + ": transparencia incorreta")
            cw, ch = width // 4, height // 3
            for index in range(12):
                left, top = index % 4 * cw, index // 4 * ch
                # Apenas leitura da regiao para analise, sem criar assets derivados.
                region = alpha.crop((left, top, left + cw, top + ch))
                mask = region.point(lambda value: 255 if value >= 128 else 0)
                box = mask.getbbox()
                check(box is not None, name + ": celula vazia " + str(index))
                x0, y0, x1, y1 = box
                check(x0 > 0 and y0 > 0 and x1 < cw and y1 < ch, name + ": sprite cortado " + str(index))
                rows = range(max(y0, y1 - 6), y1)
                xs = [x + 0.5 for y in rows for x in range(x0, x1) if region.getpixel((x, y)) >= 128]
                frame = {"x": left + x0 - 1, "y": top + y0 - 1, "width": x1 - x0 + 2, "height": y1 - y0 + 2,
                         "footX": (sum(xs) / len(xs) - x0 + 1) / (x1 - x0 + 2)}
                check(0 < frame["footX"] < 1, name + ": ancora fora do sprite")
                entry["frames"].append(frame)
            entry["maxHeight"] = max(frame["height"] for frame in entry["frames"])
        result[name] = entry

if len(sys.argv) > 1:
    output = Path(sys.argv[1]); output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result), encoding="utf-8")
print(f"Atlas e importadores: {checks} verificacoes passaram.")
