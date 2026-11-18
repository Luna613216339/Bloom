# -*- coding: utf-8 -*-
"""并行跑 提示词/清单.tsv 里的出图任务。用法（工程根目录）：python3 Docs/美术方案/工具/run_all.py [并行数] [只跑的项前缀...]
已经出齐三张图的方案自动跳过，所以断了可以直接重跑。"""
import os, sys, subprocess, glob
from concurrent.futures import ThreadPoolExecutor
here = os.path.dirname(os.path.abspath(__file__))
n = int(sys.argv[1]) if len(sys.argv) > 1 else 5
only = sys.argv[2:]
sys.path.insert(0, here)
from schemes import ITEMS
want = {}
for it in ITEMS:
    for sc in it['schemes']:
        want[f"{it['id']}-{sc['key']}"] = (f"Docs/美术方案/{it['dir']}/{sc['key']}-{sc['name']}", len(it['levels']))
if os.path.exists(os.path.join(here, 'ui.py')):
    from ui import UI
    for it in UI:
        for sc in it['schemes']:
            want[f"{it['id']}-{sc['key']}"] = (f"Docs/美术方案/{it['dir']}/{sc['key']}-{sc['name']}", len(it['shots']))
lines = [l.rstrip('\n') for l in open(os.path.join(here, '..', '提示词', '清单.tsv'), encoding='utf-8') if l.strip()]
seen, todo = set(), []
for l in lines:
    tag = os.path.basename(l.split('\t')[0])[:-4]
    if tag in seen: continue
    seen.add(tag)
    if only and not any(tag.startswith(o) for o in only): continue
    d, k = want[tag]
    if len(glob.glob(d + '/*.png')) + len(glob.glob(d + '/*.jpg')) >= k: continue
    todo.append(l)
print(len(todo), 'tasks to run', flush=True)
def run(l):
    r = subprocess.run([os.path.join(here, '出图.sh'), l], capture_output=True, text=True)
    print(r.stdout.strip(), flush=True)
with ThreadPoolExecutor(n) as ex:
    list(ex.map(run, todo))
print('ALL DONE', flush=True)
