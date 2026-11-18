# -*- coding: utf-8 -*-
"""把一项的 A/B/C × 三关拼成一张对照图，AI 自查用。用法：python3 sheet.py 1.1 输出.png"""
import os, sys, glob
from PIL import Image, ImageDraw
here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here)
from schemes import ITEMS, LEVELS, BASE
root = os.path.join(here, '..')
it = next(i for i in ITEMS if i['id'] == sys.argv[1])
W, H = 640, 360
sheet = Image.new('RGB', (3 * W, 3 * H), 'white')
d = ImageDraw.Draw(sheet)
for r, sc in enumerate(it['schemes']):
    for c, lv in enumerate(it['levels']):
        p = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}", f"{lv}-{LEVELS[lv][0]}.png")
        if not os.path.exists(p) and os.path.exists(p[:-4] + '.jpg'): p = p[:-4] + '.jpg'
        if not os.path.exists(p) and sc.get('baseline') and lv in BASE: p = os.path.join(root, '..', '..', BASE[lv])
        if not os.path.exists(p): continue
        im = Image.open(p).convert('RGB').resize((W, H), Image.LANCZOS)
        sheet.paste(im, (c * W, r * H))
        d.text((c * W + 6, r * H + H - 16), f"{sc['key']} {lv}", fill=(200, 0, 0))
sheet.save(sys.argv[2])
