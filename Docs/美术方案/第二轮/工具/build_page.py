# -*- coding: utf-8 -*-
"""生成第二轮审核页（发布成 Artifact 用）。用法（工程根目录）：python3 Docs/美术方案/第二轮/工具/build_page.py <输出目录>
输出目录里得到 index.html 和 m/ 下的全部图（改成短的英文文件名），再打印要随页面一起发布的文件清单。"""
import os, sys, json, shutil, glob, html
here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here)
import round2 as R
from ui2 import UI
root = os.path.abspath(os.path.join(here, '..'))
old = os.path.abspath(os.path.join(root, '..'))
out = sys.argv[1]; os.makedirs(os.path.join(out, 'm'), exist_ok=True)
for f in glob.glob(os.path.join(out, 'm', '*')): os.remove(f)
files = []
def pub(src):
    if not os.path.exists(src): return None
    name = f"m/{len(files) + 1}{os.path.splitext(src)[1]}"
    shutil.copyfile(src, os.path.join(out, name)); files.append(name); return name

items = []
def add_game(it):
    shots = []
    for sh in it['shots']:
        no, zh = R.LV[sh['level']]
        base = f"{no}-{zh}" + (f"-{sh['tag']}" if sh['tag'] else '')
        shots.append(dict(base=base, label=f"第 {int(no)} 关 · {zh}" + (f"（{sh['tag']}）" if sh['tag'] else ''), note=sh['note'], anim=sh['dur'] > 0))
    schemes = []
    for sc in it['schemes']:
        d = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}")
        imgs = [pub(os.path.join(d, s['base'] + ('.webp' if s['anim'] else '.jpg'))) for s in shots]
        schemes.append(dict(key=sc['key'], name=sc['name'], idea=sc['idea'], imgs=imgs))
    items.append(dict(id=it['id'], cat=it['cat'], name=it['name'], kind=it['kind'], feedback=it['feedback'], decides=it['decides'],
                      shots=[dict(label=s['label'], note=s['note']) for s in shots], schemes=schemes,
                      recommend=it['recommend'], why=it['why']))
for it in R.ITEMS: add_game(it)
for it in UI:
    schemes = []
    for sc in it['schemes']:
        d = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}")
        schemes.append(dict(key=sc['key'], name=sc['name'], idea=sc['idea'], imgs=[pub(os.path.join(d, n + '.jpg')) for n, _ in it['shots']]))
    items.append(dict(id=it['id'], cat='四、界面', name=it['name'], kind=it['kind'], feedback=it['feedback'], decides=it['decides'],
                      shots=[dict(label=n, note='') for n, _ in it['shots']], schemes=schemes, recommend=it['recommend'], why=it['why']))

walls_img = dict(A=pub(os.path.join(old, '02-元素造型/2.5-墙和笼子/A-实心墨条/13-收网.jpg')),
                 B=pub(os.path.join(old, '02-元素造型/2.5-墙和笼子/B-空心描边/07-闸.jpg')),
                 C=pub(os.path.join(old, '02-元素造型/2.5-墙和笼子/C-斜纹剖面/17-漂流.jpg')))
data = dict(items=items, decided=R.DECIDED, walls=R.WALLS, intros=R.INTROS, wallsImg=walls_img)
page = open(os.path.join(here, 'page.html'), encoding='utf-8').read().replace('/*DATA*/null', json.dumps(data, ensure_ascii=False))
open(os.path.join(out, 'index.html'), 'w', encoding='utf-8').write(page)
missing = [f"{i['id']}-{s['key']}" for i in items for s in i['schemes'] if None in s['imgs']]
print(json.dumps(files)); print('missing:', missing, file=sys.stderr)
print('total MB', round(sum(os.path.getsize(os.path.join(out, f)) for f in files) / 1e6, 1), file=sys.stderr)
