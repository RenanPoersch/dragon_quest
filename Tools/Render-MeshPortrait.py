"""Renderiza a malha original exportada por Test-Oblique como PNG transparente.

Uso: python Tools/Render-MeshPortrait.py DragonPortrait.json Assets/Resources/DragonPortrait.png
Dependencias: numpy, Pillow. O JSON fica no WorkDirectory de Test-Oblique.ps1.
"""
import json
import sys
from pathlib import Path
import numpy as np
from PIL import Image

data = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
vertices = np.asarray(data["vertices"], dtype=float)
colors = np.asarray(data["colors"], dtype=float)
faces = np.asarray(data["triangles"]).reshape(-1, 3)
width, height = 512, 384
scale = height / (2 * data["size"])
x, y = vertices[:, 0] - data["focus"][0], vertices[:, 1] - data["focus"][1]
ground_x = x * data["yawCos"] + y * data["yawSin"]
ground_y = -x * data["yawSin"] + y * data["yawCos"]
z = vertices[:, 2] + data.get("focusHeight", 0)
screen = np.column_stack((width / 2 + ground_x * scale,
    height / 2 - (ground_y * data["groundScale"] - z * data["heightScale"]) * scale))
depth = ground_y * data["heightScale"] + z * data["groundScale"]
buffer = np.full((height, width), np.inf)
rgba = np.zeros((height, width, 4), dtype=np.uint8)
for face in faces:
    a, b, c = screen[face]
    xmin, xmax = max(0, int(np.floor(min(a[0], b[0], c[0])))), min(width-1, int(np.ceil(max(a[0], b[0], c[0]))))
    ymin, ymax = max(0, int(np.floor(min(a[1], b[1], c[1])))), min(height-1, int(np.ceil(max(a[1], b[1], c[1]))))
    if xmin > xmax or ymin > ymax:
        continue
    determinant = (b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(determinant) < 1e-7:
        continue
    yy, xx = np.mgrid[ymin:ymax+1, xmin:xmax+1]
    xx, yy = xx+0.5, yy+0.5
    u = ((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/determinant
    v = ((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/determinant
    w = 1-u-v
    pixel_depth = u*depth[face[0]]+v*depth[face[1]]+w*depth[face[2]]
    region = buffer[ymin:ymax+1, xmin:xmax+1]
    visible = (u >= 0)&(v >= 0)&(w >= 0)&(pixel_depth <= region)
    region[visible] = pixel_depth[visible]
    rgba[ymin:ymax+1, xmin:xmax+1][visible] = np.append((colors[face[0]]*255).astype(np.uint8), 255)
image = Image.fromarray(rgba)
if len(sys.argv)>3 and sys.argv[3]=="--crop":
    bounds = image.getchannel("A").getbbox()
    if bounds:
        image = image.crop((max(0,bounds[0]-12),max(0,bounds[1]-12),min(width,bounds[2]+12),min(height,bounds[3]+12)))
image.save(sys.argv[2])
print("Retrato original da malha salvo em " + sys.argv[2])
