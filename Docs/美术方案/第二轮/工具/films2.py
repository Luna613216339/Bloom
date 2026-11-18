# -*- coding: utf-8 -*-
"""录第二轮的图。用法（工程根目录）：python3 Docs/美术方案/第二轮/工具/films2.py [项 id 前缀 或 项-方案 ...]
每个（项, 方案）开一个自动测试包进程（-look <BASE>,<方案的开关>），按 round2.ITEMS 里的 shots 截图 / 连拍。
静态图存成 1600×900 的 JPG，动图存成 960×540 的 WebP 动画（颜色比 GIF 准，淡的涟漪不会被压成色块）。
测试包带 -batchmode 自动静音。临时帧放在 $TMPDIR/bloom-films2 下。"""
import os, sys, json, subprocess, glob, shutil, tempfile
from concurrent.futures import ThreadPoolExecutor
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here)
from round2 import ITEMS, LV, BASE
root = os.path.abspath(os.path.join(here, '..'))
proj = os.path.abspath(os.path.join(root, '..', '..', '..'))
app = os.path.join(proj, 'Builds/Autotest/Bloom.app/Contents/MacOS/Bloom')
tmp = os.environ.get('FILMTMP') or os.path.join(tempfile.gettempdir(), 'bloom-films2')
FPS = 20
only = sys.argv[1:]

def fname(sh):
    no, zh = LV[sh['level']]
    return f"{no}-{zh}" + (f"-{sh['tag']}" if sh['tag'] else '')

def groups(it, sc):
    """同一个方案里额外开关不同的图要分开跑（开关是进程级的）"""
    g = {}
    for i, sh in enumerate(it['shots']): g.setdefault(sh['look'], []).append((i, sh))
    return g

def run(job):
    it, sc = job
    tag = f"{it['id']}-{sc['key']}"
    outdir = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}"); os.makedirs(outdir, exist_ok=True)
    for extra, shots in groups(it, sc).items():
        work = os.path.join(tmp, tag + ('-' + extra.replace('=', '') if extra else '')); shutil.rmtree(work, ignore_errors=True); os.makedirs(work)
        zmax = max([(sh['zoom'] or (0, 0, 1.0))[2] for _, sh in shots])
        still = all(sh['dur'] == 0 for _, sh in shots)
        bw, bh = (1600, 900) if still else (960, 540)
        rw, rh = int(bw * zmax), int(bh * zmax)
        jobs = []
        for i, sh in shots:
            name = f's{i}'
            if sh['dur'] > 0: act = dict(t=sh['start'], type='film', name=name, x=sh['dur'], y=FPS)
            else: act = dict(t=sh['start'], type='shot', name=name)
            jobs.append(dict(name=f'{tag} {name}', id=sh['level'], actions=[dict(a) for a in sh['acts']] + [act],
                             maxTime=sh['start'] + sh['dur'] + 0.6))
        plan = os.path.join(work, 'plan.json')
        json.dump(dict(jobs=jobs), open(plan, 'w'), ensure_ascii=False)
        look = ','.join(x for x in (BASE, sc['look'], extra) if x)
        subprocess.run([app, '-batchmode', '-mute', '-look', look, '-autotest', plan, '-autoout', os.path.join(work, 'r.json'),
                        '-autoshots', work, '-autoshotsize', f'{rw}x{rh}', '-logFile', os.path.join(work, 'log.txt')],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        for i, sh in shots:
            cx, cy, z = sh['zoom'] or (0.5, 0.5, 1.0)
            cw, ch = rw / zmax * (zmax / z), rh / zmax * (zmax / z)
            cw, ch = rw / z, rh / z
            x0 = min(max(cx * rw - cw / 2, 0), rw - cw); y0 = min(max(cy * rh - ch / 2, 0), rh - ch)
            box = (int(x0), int(y0), int(x0 + cw), int(y0 + ch))
            if sh['dur'] == 0:
                f = os.path.join(work, f's{i}.png')
                if not os.path.exists(f): print('NO SHOT', tag, fname(sh)); continue
                Image.open(f).convert('RGB').crop(box).resize((1600, 900), Image.LANCZOS).save(
                    os.path.join(outdir, fname(sh) + '.jpg'), quality=90)
                continue
            frames = sorted(glob.glob(os.path.join(work, f's{i}-*.png')))
            if not frames: print('NO FRAMES', tag, fname(sh)); continue
            ims = [Image.open(f).convert('RGB').crop(box).resize((960, 540), Image.LANCZOS) for f in frames]
            durs = [1000 // FPS] * len(ims); durs[-1] = 900
            ims[0].save(os.path.join(outdir, fname(sh) + '.webp'), save_all=True, append_images=ims[1:], duration=durs,
                        loop=0, quality=82, method=4)
            # 中间一帧留一张静态图：AI 自查看它，动图加载不出来时页面也拿它垫底
            ims[min(len(ims) - 1, int(len(ims) * 0.4))].save(os.path.join(outdir, fname(sh) + '.jpg'), quality=85)
    return tag

jobs = [(it, sc) for it in ITEMS for sc in it['schemes']
        if not only or any(f"{it['id']}-{sc['key']}".startswith(o) for o in only)]
with ThreadPoolExecutor(5) as ex:
    for tag in ex.map(run, jobs): print('done', tag, flush=True)
