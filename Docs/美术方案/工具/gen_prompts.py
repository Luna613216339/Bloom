# -*- coding: utf-8 -*-
"""按 schemes.py 生成每个方案的 Codex 任务说明（提示词/<项>-<方案>.txt）和任务清单（提示词/清单.tsv）。
清单每行：任务文件 <TAB> 参考图1 <TAB> 参考图2 ...（都是相对工程根目录的路径）"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from schemes import ITEMS, LEVELS, BASE, FLY

ROOT = 'Docs/美术方案'
here = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
common = open(os.path.join(here, '提示词', '共同要求.txt'), encoding='utf-8').read()
only = set(sys.argv[1:])
lines = []
for it in ITEMS:
    for sc in it['schemes']:
        tag = f"{it['id']}-{sc['key']}"
        if sc.get('baseline') and it['levels'] == ['34', '33', '13']:
            continue    # 基准本身：直接用第四轮 24–26
        if only and tag not in only and it['id'] not in only:
            continue
        outdir = f"{ROOT}/{it['dir']}/{sc['key']}-{sc['name']}"
        os.makedirs(os.path.join(here, '..', '..', outdir), exist_ok=True)
        refs, body = [], []
        style_needed = False
        for lv in it['levels']:
            zh, en, shot, desc = LEVELS[lv]
            out = f"{outdir}/{lv}-{zh}.png"
            if it.get('fly'):
                ref = f"{ROOT}/参考/{FLY[lv]}"; kind = '游戏截图'
                desc = '连锁进行中的一刻：分裂球刚被点着，正在膨胀成朱红实心圆；它裂出来的两颗火星正沿着各自的瞄准线飞到半路（位置照截图里那两颗拖着尾巴的小点）。旁边已经着火的球是膨胀开的朱红圆，其余球静止。'
                style_needed = True
            elif lv in BASE:
                ref = BASE[lv]; kind = '基准图'
            else:
                ref = f"{ROOT}/参考/{shot}"; kind = '游戏截图'
                style_needed = True
            refs.append(ref)
            note = (it.get('notes') or {}).get(lv, '')
            body.append(f"- 第 {int(lv)} 关「{zh}」（界面左上角写 \"{int(lv)}  {en}\"）\n"
                        f"  参考（{kind}）：{ref}\n"
                        f"  画面：{desc}{(' ' + note) if note else ''}\n"
                        f"  保存到：{out}")
        style = ''
        if style_needed:
            sref = BASE['13'] if it['id'] in ('1.3', '2.1', '2.5', '2.6') else BASE['34']
            if sref not in refs:
                refs.append(sref)
            style = f"\n风格基准图（只看画风：底色、球、影子、界面字；不要照它的构图）：{sref}\n"
        task = (f"{common}\n==== 本任务 ====\n"
                f"项目：{it['id']} {it['name']} —— {it['decides']}\n"
                f"你负责的方案：{sc['key']}「{sc['name']}」\n"
                f"这个方案要改的（只改这一条，其余全部照暂定基准）：\n{sc['change']}\n"
                f"{style}\n三张图：\n" + "\n".join(body) + "\n\n"
                f"三张图要像同一款游戏同一套美术里截下来的。画完自己检查一遍：有没有违反\"永远不能违反\"的任何一条；除了本方案要改的那一条，有没有别的地方偏离了暂定基准。\n")
        path = os.path.join(here, '提示词', f'{tag}.txt')
        open(path, 'w', encoding='utf-8').write(task)
        lines.append('\t'.join([f"{ROOT}/提示词/{tag}.txt"] + refs))
mode = 'a' if only else 'w'
open(os.path.join(here, '提示词', '清单.tsv'), mode, encoding='utf-8').write('\n'.join(lines) + '\n')
print(len(lines), 'tasks')
