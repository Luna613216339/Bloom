# -*- coding: utf-8 -*-
"""生成 四、界面 的 Codex 任务说明，追加到 提示词/清单.tsv"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from ui import UI
ROOT = 'Docs/美术方案'
here = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
common = open(os.path.join(here, '提示词', '共同要求.txt'), encoding='utf-8').read()
common = common.replace('三张图（三关各一张）', '下面列出的几张图').replace('- 界面：左上角"关号 + 英文关卡名"（小号细无衬线体，墨青色），右上角一个细线暂停圆钮。别的字一概不要。',
    '- 界面（局内）：左上角"关号 + 英文关卡名"（小号细无衬线体，墨青色），右上角一个细线暂停圆钮。').replace(
    '- 不要水印、边框、标签、箭头、说明文字、图例、分格、对比拼图。每张图就是一张游戏画面。',
    '- 不要水印、边框、标签、说明文字、图例、分格、对比拼图。每张图就是一张游戏画面（一个界面）。除了任务点名的字，别的字一概不要；字必须拼写正确。').replace(
    '最后在同一个方案文件夹里写一个 `提示词.txt`，记下三张图实际用的图像提示词（英文原文）。', '最后在同一个方案文件夹里写一个 `提示词.txt`，记下每张图实际用的图像提示词（英文原文）。')
lines = []
for it in UI:
    for sc in it['schemes']:
        if sc.get('baseline'): continue
        tag = f"{it['id']}-{sc['key']}"
        outdir = f"{ROOT}/{it['dir']}/{sc['key']}-{sc['name']}"
        os.makedirs(os.path.join(here, '..', '..', outdir), exist_ok=True)
        refs, body = [], []
        for (name, ref, desc) in it['shots']:
            if ref and ref not in refs: refs.append(ref)
            body.append(f"- 「{name}」\n  参考（基准图，关卡画面照它）：{ref or '无（新画面，画风照风格基准图）'}\n  画面：{desc}\n  保存到：{outdir}/{name}.png")
        style = ''
        if not refs:
            refs.append('Docs/效果图/第四轮/24.png')
            style = "\n风格基准图（只看画风：底色、球、影子、字的气质；不要照它的构图）：Docs/效果图/第四轮/24.png\n"
        task = (f"{common}\n==== 本任务（界面）====\n"
                f"项目：{it['id']} {it['name']} —— {it['decides']}\n"
                f"你负责的方案：{sc['key']}「{sc['name']}」\n"
                f"这个方案的设计（界面照这一条画；关卡画面、配色、球、影子等其余全部照暂定基准）：\n{sc['change']}\n"
                f"{style}\n要画的图：\n" + "\n".join(body) + "\n\n"
                f"每张图只生成一次；只有字拼错、或明显违反\"永远不能违反\"时才重画，整个任务最多重画一张。画完自己检查一遍。\n")
        open(os.path.join(here, '提示词', f'{tag}.txt'), 'w', encoding='utf-8').write(task)
        lines.append('\t'.join([f"{ROOT}/提示词/{tag}.txt"] + refs))
open(os.path.join(here, '提示词', '清单.tsv'), 'a', encoding='utf-8').write('\n'.join(lines) + '\n')
print(len(lines), 'ui tasks')
