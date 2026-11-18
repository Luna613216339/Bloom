# -*- coding: utf-8 -*-
"""并行跑 提示词/清单.tsv 里的界面出图任务（工程根目录运行）：python3 Docs/美术方案/第二轮/工具/run_ui2.py [并行数] [项前缀...]
调的是上一轮的 工具/出图.sh（Codex，gpt-6.1-sol + high）。已经出齐图的方案自动跳过，断了可以直接重跑。出完把 PNG 压成 JPG。"""
import os, sys, subprocess, glob
from concurrent.futures import ThreadPoolExecutor
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here)
from ui2 import UI
root = 'Docs/美术方案/第二轮'
n = int(sys.argv[1]) if len(sys.argv) > 1 else 5
only = sys.argv[2:]
want = {f"{it['id']}-{sc['key']}": (f"{root}/{it['dir']}/{sc['key']}-{sc['name']}", len(it['shots'])) for it in UI for sc in it['schemes']}
todo = []
for l in open(f'{root}/提示词/清单.tsv', encoding='utf-8').read().splitlines():
    tag = os.path.basename(l.split('\t')[0])[:-4]
    if only and not any(tag.startswith(o) for o in only): continue
    d, k = want[tag]
    if len(glob.glob(d + '/*.png')) + len(glob.glob(d + '/*.jpg')) >= k: continue
    todo.append(l)
print(len(todo), 'tasks to run', flush=True)
def run(l):
    r = subprocess.run(['Docs/美术方案/工具/出图.sh', l], capture_output=True, text=True)
    print(r.stdout.strip(), flush=True)
with ThreadPoolExecutor(n) as ex: list(ex.map(run, todo))
for d, _ in want.values():
    for p in glob.glob(d + '/*.png'):
        Image.open(p).convert('RGB').resize((1600, 900), Image.LANCZOS).save(p[:-4] + '.jpg', quality=88); os.remove(p)
print('ALL DONE', flush=True)
