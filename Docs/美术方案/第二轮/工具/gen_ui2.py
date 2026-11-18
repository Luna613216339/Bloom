# -*- coding: utf-8 -*-
"""生成第二轮界面的出图任务（工程根目录运行）：python3 Docs/美术方案/第二轮/工具/gen_ui2.py
每个方案一个任务文件 提示词/<项>-<方案>.txt，清单写进 提示词/清单.tsv（任务文件 <TAB> 参考图...）。"""
import os, sys
here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here)
from ui2 import UI
root = 'Docs/美术方案/第二轮'
common = open(os.path.join(root, '提示词/共同要求.txt'), encoding='utf-8').read()
lines = []
for it in UI:
    for sc in it['schemes']:
        refs = sc.get('refs') or it.get('refs') or []
        out = f"{root}/{it['dir']}/{sc['key']}-{sc['name']}"
        os.makedirs(out, exist_ok=True)
        t = [common, '', '==== 本任务 ====', f"界面：{it['id']} {it['name']} —— {it['decides']}", f"你负责的方案：{sc['key']}「{sc['name']}」",
             f"这个方案的做法：{sc['change']}", '', '参考图（按 -i 传入的顺序）：']
        t += [f"  {i + 1}. {r}" for i, r in enumerate(refs)]
        if it.get('refs'): t.append('  这些是游戏截图，一张对应下面一张要画的图（顺序相同）。')
        t += ['', '要画的图：']
        for name, desc in it['shots']:
            t.append(f"- 保存为 {out}/{name}.png ：{desc}")
        path = f"{root}/提示词/{it['id']}-{sc['key']}.txt"
        open(path, 'w', encoding='utf-8').write('\n'.join(t) + '\n')
        lines.append('\t'.join([path] + refs))
open(f'{root}/提示词/清单.tsv', 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
print(len(lines), 'tasks')
