# -*- coding: utf-8 -*-
"""抓动画方案的动图。用法（工程根目录）：python3 Docs/美术方案/工具/films.py [项 id 前缀...]
每个（项, 方案）开一个自动测试包进程（-look skin=1,<key>=<A|B|C>），按 schemes.ANIMS 里的 film 连拍，再用 PIL 拼成 960×540 的 GIF。
测试包带 -batchmode 自动静音。临时帧放在 $TMPDIR/bloom-films 下。"""
import os, sys, json, subprocess, glob, shutil, tempfile
from concurrent.futures import ThreadPoolExecutor
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here)
from schemes import ANIMS, LEVELS, ZOOMS
root = os.path.abspath(os.path.join(here, '..'))
proj = os.path.abspath(os.path.join(root, '..', '..'))
app = os.path.join(proj, 'Builds/Autotest/Bloom.app/Contents/MacOS/Bloom')
tmp = os.environ.get('FILMTMP') or os.path.join(tempfile.gettempdir(), 'bloom-films')
FPS = 20
only = sys.argv[1:]

def run(job):
    it, sc = job
    tag = f"{it['id']}-{sc['key']}"
    work = os.path.join(tmp, tag); shutil.rmtree(work, ignore_errors=True); os.makedirs(work)
    jobs = []
    for (lid, lv, acts, start, dur) in it['films']:
        a = [dict(x) for x in acts] + [dict(t=start, type='film', name=lv, x=dur, y=FPS)]
        jobs.append(dict(name=f'{tag} {lv}', id=lid, actions=a, maxTime=start + dur + 1))
    zmax = max([ZOOMS.get((it['id'], f[1]), (0, 0, 1.0))[2] for f in it['films']])
    rw, rh = int(960 * zmax), int(540 * zmax)
    plan = os.path.join(work, 'plan.json')
    json.dump(dict(jobs=jobs), open(plan, 'w'), ensure_ascii=False)
    subprocess.run([app, '-batchmode', '-mute', '-look', f"skin=1,{it['key']}={sc['key']}",
                    '-autotest', plan, '-autoout', os.path.join(work, 'r.json'), '-autoshots', work,
                    '-autoshotsize', f'{rw}x{rh}', '-logFile', os.path.join(work, 'log.txt')],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    outdir = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}"); os.makedirs(outdir, exist_ok=True)
    for (lid, lv, acts, start, dur) in it['films']:
        frames = sorted(glob.glob(os.path.join(work, f'{lv}-*.png')))
        if not frames: print('NO FRAMES', tag, lv); continue
        cx, cy, z = ZOOMS.get((it['id'], lv), (0.5, 0.5, 1.0))
        cw, ch = rw * z / zmax / z * 1.0, rh * z / zmax / z * 1.0      # 裁切窗口（渲染像素）= 整幅 / z
        cw, ch = rw / z, rh / z
        x0 = min(max(cx * rw - cw / 2, 0), rw - cw); y0 = min(max(cy * rh - ch / 2, 0), rh - ch)
        box = (int(x0), int(y0), int(x0 + cw), int(y0 + ch))
        ims = [Image.open(f).convert('RGB').crop(box).resize((960, 540), Image.LANCZOS) for f in frames]
        # 一套共用调色板：从头、中、尾几帧拼一张图取色，颜色才不会一帧一跳
        k = [ims[i] for i in sorted(set([0, len(ims)//4, len(ims)//2, 3*len(ims)//4, len(ims)-1]))]
        strip = Image.new('RGB', (960 * len(k), 540))
        for i, im in enumerate(k): strip.paste(im, (960 * i, 0))
        pal = strip.quantize(colors=96, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
        ps = [im.quantize(palette=pal, dither=Image.Dither.NONE) for im in ims]
        durs = [1000 // FPS] * len(ps); durs[-1] = 900
        out = os.path.join(outdir, f"{lv}-{LEVELS[lv][0]}.gif")
        ps[0].save(out, save_all=True, append_images=ps[1:], duration=durs, loop=0, optimize=True)
        # 中间一帧留一张静态图，报告里 GIF 没加载出来时也有东西看，AI 自查也看它
        ims[min(len(ims) - 1, int(len(ims) * 0.45))].save(os.path.join(work, f'{lv}-mid.png'))
    return tag

jobs = [(it, sc) for it in ANIMS for sc in it['schemes']
        if not only or any(f"{it['id']}-{sc['key']}".startswith(o) for o in only)]
with ThreadPoolExecutor(4) as ex:
    for tag in ex.map(run, jobs): print('done', tag, flush=True)
