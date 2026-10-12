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

for name in ("ArenWalk", "ArenDiagonalWalk", "LiaWalk", "BramWalk", "NpcRoster", "TerrainAtlas", "CityMaterialsAtlas", "CityDetailsAtlas", "CastleMaterialsAtlas", "CityGroundMap", "CityGroundSurroundings"):
    path = directory / (name + ".png")
    with Image.open(path) as image:
        width, height = image.size
        ground_map = name in ("CityGroundMap", "CityGroundSurroundings")
        map_width, map_height = (62, 80) if name == "CityGroundSurroundings" else (26, 44)
        check(width >= 16 and height >= 12 and (abs(width * map_height - height * map_width) / (height * map_width) < .02 if ground_map
              else abs(width * 3 - height * 4) <= 8), name + ": proporcao irregular")
        check(width <= (4096 if ground_map else 2048) and height <= (4096 if ground_map else 2048), name + ": tamanho acima do importador")
        meta = path.with_suffix(".png.meta").read_text(encoding="utf-8-sig")
        for setting in ("filterMode: 0", "enableMipMap: 0", "nPOTScale: 0", "textureCompression: 0"):
            check(setting in meta, name + ": importacao incorreta " + setting)
        entry = {"width": width, "height": height, "frames": []}
        if ground_map:
            check(image.mode in ("RGB", "RGBA") and (image.mode == "RGB" or image.getchannel("A").getextrema() == (255, 255)), name + ": mapa deve ser opaco")
            for setting in ("wrapU: 1", "wrapV: 1", "maxTextureSize: 4096"):
                check(setting in meta, name + ": mapa inteiro sem repeticao ou reducao " + setting)
        elif name not in ("TerrainAtlas", "CityMaterialsAtlas", "CityDetailsAtlas", "CastleMaterialsAtlas"):
            check(image.mode == "RGBA", name + ": canal alpha ausente")
            check("isReadable: 1" in meta, name + ": alpha precisa ser legivel em runtime")
            alpha = image.getchannel("A")
            check(alpha.getextrema() == (0, 255), name + ": transparencia incorreta")
            for index in range(12):
                column, row = index % 4, index // 4
                left, top = column * width // 4, row * height // 3
                right, bottom = (column + 1) * width // 4, (row + 1) * height // 3
                # Apenas leitura da regiao para analise, sem criar assets derivados.
                region = alpha.crop((left, top, right, bottom))
                mask = region.point(lambda value: 255 if value >= 128 else 0)
                box = mask.getbbox()
                check(box is not None, name + ": celula vazia " + str(index))
                x0, y0, x1, y1 = box
                check(x0 > 0 and y0 > 0 and x1 < right - left and y1 < bottom - top, name + ": sprite cortado " + str(index))
                # Mesma ancora usada em runtime: tronco estavel para Aren; pes para moradores estaticos.
                walking_sheet = name in ("ArenWalk", "ArenDiagonalWalk")
                rows = range(y0, y0 + (y1 - y0 - 1) // 3 + 1) if walking_sheet else range(max(y0, y1 - 6), y1)
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
