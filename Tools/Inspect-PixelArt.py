"""Valida atlas e importadores; exporta metadados para previas sem modificar imagens."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageChops

root = Path(__file__).resolve().parents[1]
directory = root / "Assets/Resources/PixelArt"
result = {}
checks = 0

def check(condition, message):
    global checks
    checks += 1
    if not condition:
        raise ValueError(message)

for name in ("ArenWalk", "ArenDiagonalWalk", "LiaWalk", "BramWalk", "NpcRoster", "ChestStates", "TerrainAtlas", "CityMaterialsAtlas", "CityDetailsAtlas", "CastleMaterialsAtlas", "CityGroundMap", "WorldGroundMap", "WorldPoiSprites"):
    path = directory / (name + ".png")
    with Image.open(path) as image:
        width, height = image.size
        ground_map = name == "CityGroundMap"
        prop_pair = name == "ChestStates"
        world_map = name == "WorldGroundMap"
        world_poi = name == "WorldPoiSprites"
        map_width, map_height = (62, 80)
        check(width >= 16 and height >= 12 and (abs(width * map_height - height * map_width) / (height * map_width) < .02 if ground_map
              else width * 2 == height * 3 if world_map or world_poi else abs(width - height * 2) <= 8 if prop_pair else abs(width * 3 - height * 4) <= 8), name + ": proporcao irregular")
        check(width <= (4096 if ground_map or world_map else 2048) and height <= (4096 if ground_map or world_map else 2048), name + ": tamanho acima do importador")
        meta = path.with_suffix(".png.meta").read_text(encoding="utf-8-sig")
        for setting in ("filterMode: 0", "enableMipMap: 0", "nPOTScale: 0", "textureCompression: 0"):
            check(setting in meta, name + ": importacao incorreta " + setting)
        entry = {"width": width, "height": height, "frames": []}
        if ground_map:
            check(width >= 2304 and height >= 2974, name + ": resolucao insuficiente para conservar o detalhe original")
            check((width, height) == (2305, 2975), name + ": dimensoes devem corresponder ao mapeamento nativo do chao")
            with Image.open(root / 'Docs/ArtInputs/CityGroundRebuilt.png') as original:
                # Leitura apenas: confere os pixels do nucleo regenerado na densidade nativa.
                core = image.crop((670, 673, 670 + 965, 673 + 1629)).convert('RGB')
                check(ImageChops.difference(core, original.convert('RGB')).getbbox() is None,
                      name + ": nucleo regenerado sofreu perda de detalhe ou mudanca de pixels")
            check(image.mode in ("RGB", "RGBA") and (image.mode == "RGB" or image.getchannel("A").getextrema() == (255, 255)), name + ": mapa deve ser opaco")
            for setting in ("wrapU: 1", "wrapV: 1", "maxTextureSize: 4096"):
                check(setting in meta, name + ": mapa inteiro sem repeticao ou reducao " + setting)
        elif world_map:
            check((width, height) == (1536, 1024), name + ": tamanho nativo da pintura mudou")
            check(image.mode in ("RGB", "RGBA") and (image.mode == "RGB" or image.getchannel("A").getextrema() == (255, 255)), name + ": mapa deve ser opaco")
            for setting in ("wrapU: 1", "wrapV: 1", "maxTextureSize: 4096"):
                check(setting in meta, name + ": pintura unica sem reducao " + setting)
        elif name not in ("TerrainAtlas", "CityMaterialsAtlas", "CityDetailsAtlas", "CastleMaterialsAtlas"):
            check(image.mode == "RGBA", name + ": canal alpha ausente")
            check("isReadable: 1" in meta, name + ": alpha precisa ser legivel em runtime")
            alpha = image.getchannel("A")
            check(alpha.getextrema()[0] == 0 and alpha.getextrema()[1] >= 250 if world_poi else alpha.getextrema() == (0, 255), name + ": transparencia incorreta")
            columns, rows_count = (2, 1) if prop_pair else (4, 3)
            base_widths, base_bottoms = [], []
            regions = [(0, 0, 550, 540), (550, 0, 1040, 540), (1040, 0, 1536, 540), (0, 540, 550, 1024), (550, 520, 1140, 1024)] if world_poi else [
                (index % columns * width // columns, index // columns * height // rows_count,
                 (index % columns + 1) * width // columns, (index // columns + 1) * height // rows_count) for index in range(columns * rows_count)]
            for index, (left, top, right, bottom) in enumerate(regions):
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
                if prop_pair:
                    base_row = round(((entry['frames'][0]['width'] if index else frame['width']) - 2) * .23)
                    body_xs = [x for x in range(x0, x1) if region.getpixel((x, y1 - 1 - base_row)) >= 128]
                    check(bool(body_xs), name + ": base ausente")
                    frame['footX'] = ((min(body_xs) + max(body_xs) + 1) / 2 - x0 + 1) / frame['width']
                    base_widths.append(max(body_xs) - min(body_xs) + 1)
                    base_bottoms.append(y1)
                if world_poi:
                    frame['footX'] = .5
                check(0 < frame["footX"] < 1, name + ": ancora fora do sprite")
                entry["frames"].append(frame)
            entry["maxHeight"] = max(frame["height"] for frame in entry["frames"])
            if world_poi:
                check((width, height) == (1536, 1024), name + ": janelas requerem a folha nativa")
                # Nenhum pixel de corpo pode ser perdido ou pertencer a duas janelas.
                coverage = Image.new('L', (width, height), 0)
                for left, top, right, bottom in regions:
                    rectangle = Image.new('L', (right-left, bottom-top), 1)
                    coverage.paste(ImageChops.add(coverage.crop((left, top, right, bottom)), rectangle), (left, top))
                opaque = alpha.point(lambda value: 255 if value >= 128 else 0)
                unique = coverage.point(lambda value: 255 if value == 1 else 0)
                check(ImageChops.subtract(opaque, unique).getbbox() is None, name + ": corpo perdido ou compartilhado entre icones")
                entry['pixelsPerUnit'] = entry['maxHeight'] / (1.95 * .8191520443)
            if prop_pair:
                entry['pixelsPerUnit'] = entry['frames'][0]['width'] / 1.1
                check(abs(base_widths[0] - base_widths[1]) < base_widths[0] * .04, name + ": caixa muda de largura ao abrir")
                # Cada recorte ancora a propria base a um pixel da borda inferior. A posicao
                # bruta na folha pode variar sem deslocar o bau ao trocar os sprites.
                check(all(frame['y'] + frame['height'] - bottom == 1
                          for frame, bottom in zip(entry['frames'], base_bottoms)), name + ": margem inferior da ancora incorreta")
        result[name] = entry

if len(sys.argv) > 1:
    output = Path(sys.argv[1]); output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result), encoding="utf-8")
print(f"Atlas e importadores: {checks} verificacoes passaram.")
