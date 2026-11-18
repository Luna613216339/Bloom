# -*- coding: utf-8 -*-
"""生成 Docs/美术方案/审核报告.html。用法（任意目录）：python3 Docs/美术方案/工具/build_report.py
顺带把 GPT 出的 PNG 压成 1600×900 的 JPG（一张从 1 MB 降到 100 多 KB），把"基准本身"那几个方案的图从第四轮拷过来。
报告是一个不依赖网络的单文件，图用相对路径，放在 Docs/美术方案/ 下直接双击打开。"""
import os, sys, glob, html, json
from urllib.parse import quote
from PIL import Image

here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here)
from schemes import ITEMS, ANIMS, LEVELS, BASE
from ui import UI
root = os.path.abspath(os.path.join(here, '..'))
proj = os.path.abspath(os.path.join(root, '..', '..'))

# ---------- 1. 整理图片 ----------
def to_jpg(src, dst):
    im = Image.open(src).convert('RGB')
    if im.size != (1600, 900): im = im.resize((1600, 900), Image.LANCZOS)
    im.save(dst, quality=90, subsampling=0)

for d in glob.glob(os.path.join(root, '0[124]-*', '*', '*')):
    if not os.path.isdir(d) or not os.path.exists(os.path.join(d, '提示词.txt')): continue   # 还在画的先不动
    for p in glob.glob(os.path.join(d, '*.png')):
        to_jpg(p, p[:-4] + '.jpg'); os.remove(p)

def ensure_baseline(dirpath, pairs):
    os.makedirs(dirpath, exist_ok=True)
    for name, src in pairs:
        dst = os.path.join(dirpath, name + '.jpg')
        if not os.path.exists(dst): to_jpg(os.path.join(proj, src), dst)

for it in ITEMS:
    for sc in it['schemes']:
        if sc.get('baseline') and it['levels'] == ['34', '33', '13']:
            ensure_baseline(os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}"),
                            [(f"{lv}-{LEVELS[lv][0]}", BASE[lv]) for lv in it['levels']])
for it in UI:
    for sc in it['schemes']:
        if sc.get('baseline'):
            ensure_baseline(os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}"), [(n, r) for (n, r, _) in it['shots']])

# ---------- 2. 每张图上的放大镜看哪里（占画面宽、高的百分比）----------
FOCUS = {
    '1.3': {'13': [(50, 42)], '07': [(52, 50)], '14': [(60, 33)]},
    '2.1': {'13': [(50, 60)], '14': [(27, 72)], '15': [(18, 47)]},
    '2.2': {'34': [(28, 50), (72, 50)], '31': [(50, 29)], '35': [(48, 50)]},
    '2.3': {'34': [(50, 50)], '19': [(50, 57)], '20': [(33, 50)]},
    '2.4': {'34': [(38, 50)], '19': [(41, 38)], '26': [(59, 25)]},
    '2.5': {'13': [(50, 28)], '07': [(52, 50)], '17': [(10, 10)]},
    '2.6': {'13': [(50, 44)], '07': [(52, 50)], '24': [(50, 26), (50, 82)]},
    '2.7': {'34': [(62, 50)], '04': [(50, 45)], '26': [(59, 43)]},
}
ZOOM, BOX = 3.4, 0.27     # 放大倍数；放大镜直径占图宽的比例

def rel(p): return quote(os.path.relpath(p, root))
def esc(s): return html.escape(s, quote=True)

def find(dirpath, stem):
    for ext in ('.jpg', '.png', '.gif'):
        p = os.path.join(dirpath, stem + ext)
        if os.path.exists(p): return p
    return None

def cell(path, caption, focus=None):
    if not path:
        return f'<figure class="shot missing"><div class="ph">还没出图</div><figcaption>{esc(caption)}</figcaption></figure>'
    src = rel(path)
    lens = ''
    for i, (fx, fy) in enumerate(focus or []):
        w = ZOOM / BOX * 100                      # 图在放大镜里的宽度（相对放大镜的 %）
        left = 50 - fx / 100 * w
        top = 50 - fy / 100 * w * 9 / 16
        side = 'right' if len(focus) == 1 or i == 1 else 'left'   # 两个放大镜时按左右顺序摆
        lens += (f'<span class="lens {side}"><img loading="lazy" src="{src}" alt="" '
                 f'style="width:{w:.1f}%;left:{left:.1f}%;top:{top:.1f}%"></span>')
    return (f'<figure class="shot"><a href="{src}" target="_blank"><img loading="lazy" src="{src}" alt="{esc(caption)}">{lens}</a>'
            f'<figcaption>{esc(caption)}</figcaption></figure>')

def scheme_row(it, sc, cells, n):
    rec = ' rec' if sc['key'] == it['recommend'] else ''
    tag = '<span class="tag">推荐</span>' if rec else ''
    base = '<span class="tag base">暂定基准</span>' if sc.get('baseline') or '暂定基准' in sc['idea'] else ''
    return (f'<div class="scheme{rec}" data-item="{it["id"]}" data-key="{sc["key"]}">'
            f'<button class="pick" type="button" aria-label="选 {sc["key"]}"><b>{sc["key"]}</b></button>'
            f'<div class="body"><h4>{esc(sc["name"])}{tag}{base}</h4>'
            f'<div class="shots n{n}">{"".join(cells)}</div>'
            f'<p class="idea">{esc(sc["idea"])}</p></div></div>')

def section(it, rows, extra=''):
    names = '、'.join(f"{s['key']} {s['name']}" for s in it['schemes'])
    return (f'<section class="item" id="i{it["id"]}" data-id="{it["id"]}" data-name="{esc(it["name"])}">'
            f'<h3><span class="num">{it["id"]}</span>{esc(it["name"])}</h3>'
            f'<p class="decides">{esc(it["decides"])}</p>{extra}{"".join(rows)}'
            f'<div class="verdict"><b>我推荐 {it["recommend"]}。</b>{esc(it["why"])}</div>'
            f'<div class="none" data-item="{it["id"]}"><button class="pick none-btn" type="button">都不要</button>'
            f'<input type="text" placeholder="都不要的话，写一句你想要什么（也可以选了字母再在这里补一句）"></div>'
            f'</section>')

parts, toc = [], []

def group(title, intro):
    gid = f'g{len(toc)}'
    toc.append((gid, title, []))
    parts.append(f'<h2 id="{gid}">{esc(title)}</h2><p class="gintro">{intro}</p>')

group('一、全局基调', '先定这三项。它们决定后面所有图的底子：配色一换，二、三、四的图都要跟着换色，但不影响你现在比较造型和动作。')
for it in ITEMS:
    if it['id'] == '2.1':
        group('二、元素造型', '每张图右下角的圆是放大镜，对着这一项在比的那个东西（有两个状态时左下角还有一个）。点图可以看原图。特殊球的重点是：靠什么一眼和普通球分开。')
    rows = []
    for sc in it['schemes']:
        d = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}")
        cells = [cell(find(d, f"{lv}-{LEVELS[lv][0]}"), f"第 {int(lv)} 关 · {LEVELS[lv][0]}",
                      FOCUS.get(it['id'], {}).get(lv)) for lv in it['levels']]
        rows.append(scheme_row(it, sc, cells, 3))
    extra = ''
    if it.get('notes'):
        extra = '<p class="note">图里的状态：' + '　'.join(f"第 {int(k)} 关——{esc(v.replace('这张图里', '').replace('：', '').replace('【', '').replace('】', '').strip('。'))}" for k, v in it['notes'].items()) + '。</p>'
    parts.append(section(it, rows, extra))
    toc[-1][2].append(it)

group('三、动画', '这一类不是效果图，是游戏里真的做出来、用自动测试包录的动图（会循环播放）。为了只看动作，画面用的是暂定基准的颜色，没有加影子和圆角。'
      '每一项的 A 都是游戏现在的样子。三版都只改了外观：36 关的解法在三种配置下全部照样通过，收到的球数和结算时间一帧不差。')
for it in ANIMS:
    rows = []
    for sc in it['schemes']:
        d = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}")
        cells = [cell(find(d, f"{lv}-{LEVELS[lv][0]}"), f"第 {int(lv)} 关 · {LEVELS[lv][0]}") for (_, lv, _, _, _) in it['films']]
        rows.append(scheme_row(it, sc, cells, 3))
    parts.append(section(it, rows))
    toc[-1][2].append(it)

ui_ready = [it for it in UI if all(find(os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}"), it['shots'][0][0]) for sc in it['schemes'])]
if ui_ready:
    group('四、界面', '界面是在前三类的暂定基准上画的。图里的英文是占位，正式版中英文都有。转场属于动画，等前面定了再做。')
    for it in ui_ready:
        rows = []
        for sc in it['schemes']:
            d = os.path.join(root, it['dir'], f"{sc['key']}-{sc['name']}")
            cells = [cell(find(d, n), n) for (n, _, _) in it['shots']]
            rows.append(scheme_row(it, sc, cells, len(it['shots'])))
        parts.append(section(it, rows))
        toc[-1][2].append(it)

all_items = [it for _, _, its in toc for it in its]
ui_note = '' if ui_ready else '<li><b>四、界面</b>这一版还没有：给 GPT 的额度这一轮用完了，方案和提示词已经写好，额度恢复后补出，报告会更新。</li>'

nav = ''.join(f'<a href="#{gid}">{esc(t)}</a>' + ''.join(f'<a class="sub" href="#i{it["id"]}" data-nav="{it["id"]}">{it["id"]} {esc(it["name"])}</a>' for it in its)
              for gid, t, its in toc)
template = '\n'.join(f"{it['id']} {it['name']}：__（A/B/C/都不要：____）" for it in all_items)
meta = json.dumps([dict(id=it['id'], name=it['name']) for it in all_items], ensure_ascii=False)

CSS = '''
:root{--bg:#F7F4EE;--card:#FBF9F5;--ink:#2F3B39;--soft:#6B7775;--line:#E3DDD1;--fire:#D9573F;--pick:#465A58}
*{box-sizing:border-box}
html{scroll-behavior:smooth}
body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.7 -apple-system,"PingFang SC","Hiragino Sans GB","Microsoft YaHei",sans-serif}
.wrap{display:grid;grid-template-columns:220px minmax(0,1fr);gap:32px;max-width:1560px;margin:0 auto;padding:0 28px 120px}
nav{position:sticky;top:0;align-self:start;height:100vh;overflow:auto;padding:28px 0;font-size:13.5px}
nav a{display:block;color:var(--ink);text-decoration:none;padding:5px 0;font-weight:600}
nav a.sub{font-weight:400;color:var(--soft);padding:2px 0 2px 12px;border-left:2px solid var(--line)}
nav a.sub.done{color:var(--ink);border-left-color:var(--pick)}
nav a.sub.done::after{content:" ✓";color:var(--pick)}
nav a:hover{color:var(--fire)}
main{min-width:0;padding-top:40px}
h1{font-size:30px;font-weight:600;margin:0 0 6px;letter-spacing:.02em}
h2{font-size:23px;font-weight:600;margin:72px 0 6px;padding-top:20px;border-top:1px solid var(--line)}
h3{font-size:19px;font-weight:600;margin:0 0 2px}
h3 .num{display:inline-block;min-width:2.6em;color:var(--fire);font-variant-numeric:tabular-nums}
h4{margin:0 0 8px;font-size:15.5px;font-weight:600}
p{margin:6px 0}
.lead{color:var(--soft);max-width:60em}
.gintro,.decides{color:var(--soft);max-width:62em}
.note{font-size:13.5px;color:var(--soft)}
.how{background:var(--card);border:1px solid var(--line);border-radius:14px;padding:18px 24px;margin:22px 0}
.how h3{margin-bottom:6px}
.how ul,.how ol{margin:6px 0;padding-left:1.3em;max-width:64em}
.how li{margin:3px 0}
.basegrid{display:grid;grid-template-columns:repeat(3,1fr);gap:10px;margin-top:10px}
.item{background:var(--card);border:1px solid var(--line);border-radius:14px;padding:22px 24px 18px;margin:22px 0;scroll-margin-top:16px}
.scheme{display:grid;grid-template-columns:56px minmax(0,1fr);gap:14px;padding:16px 0 10px;border-top:1px solid var(--line);margin-top:14px}
.pick{font:inherit;cursor:pointer;border:1.5px solid var(--line);background:var(--bg);color:var(--ink);border-radius:999px;transition:all .12s}
.scheme>.pick{width:48px;height:48px;font-size:20px;align-self:start;position:sticky;top:16px}
.pick:hover{border-color:var(--pick)}
.scheme.on>.pick,.none.on .none-btn{background:var(--pick);border-color:var(--pick);color:#fff}
.scheme.on{background:linear-gradient(90deg,rgba(70,90,88,.07),transparent 40%);margin-left:-12px;padding-left:12px;border-radius:10px}
.tag{font-size:12px;font-weight:500;color:var(--fire);border:1px solid var(--fire);border-radius:999px;padding:1px 8px;margin-left:10px;vertical-align:1px}
.tag.base{color:var(--soft);border-color:var(--line)}
.shots{display:grid;gap:10px}
.shots.n3{grid-template-columns:repeat(3,1fr)}
.shots.n2{grid-template-columns:repeat(2,1fr);max-width:1000px}
.shots.n1{grid-template-columns:1fr;max-width:640px}
figure{margin:0}
.shot a{display:block;position:relative;border-radius:8px;overflow:hidden;border:1px solid var(--line);background:#fff;aspect-ratio:16/9}
.shot a>img{display:block;width:100%;height:100%;object-fit:cover}
.lens{position:absolute;bottom:3%;width:27%;aspect-ratio:1;border-radius:50%;overflow:hidden;border:2px solid #fff;box-shadow:0 0 0 1px var(--line);background:#fff}
.lens.right{right:2%}.lens.left{left:2%}
.lens img{position:absolute;max-width:none;height:auto}
figcaption{font-size:12.5px;color:var(--soft);margin-top:3px}
.ph{aspect-ratio:16/9;border:1px dashed var(--line);border-radius:8px;display:grid;place-items:center;color:var(--soft);font-size:13px}
.idea{margin:8px 0 0;max-width:62em}
.verdict{margin-top:16px;padding:10px 14px;border-left:3px solid var(--fire);background:rgba(217,87,63,.06);border-radius:0 8px 8px 0;max-width:66em}
.none{display:flex;gap:10px;align-items:center;margin-top:12px}
.none-btn{padding:6px 16px;font-size:14px;white-space:nowrap}
.none input{flex:1;min-width:0;font:inherit;font-size:14px;padding:7px 12px;border:1px solid var(--line);border-radius:8px;background:var(--bg);color:var(--ink)}
.replybox{background:var(--card);border:1px solid var(--line);border-radius:14px;padding:22px 24px;margin:22px 0}
#out{width:100%;min-height:26em;font:14px/1.75 ui-monospace,"SF Mono",Menlo,monospace;padding:14px;border:1px solid var(--line);border-radius:10px;background:var(--bg);color:var(--ink);resize:vertical}
.btn{font:inherit;font-size:15px;cursor:pointer;border:0;border-radius:999px;background:var(--fire);color:#fff;padding:9px 22px;margin:10px 10px 0 0}
.btn.ghost{background:transparent;color:var(--ink);border:1.5px solid var(--line)}
#count{color:var(--soft);font-size:14px}
@media(max-width:1000px){.wrap{grid-template-columns:1fr;padding:0 16px 80px}nav{display:none}.shots.n3,.shots.n2,.basegrid{grid-template-columns:1fr}.scheme{grid-template-columns:44px minmax(0,1fr)}}
'''

JS = '''
const ITEMS = __META__;
const KEY = 'bloom-art-review-v1';
let state = {};
try { state = JSON.parse(localStorage.getItem(KEY) || '{}'); } catch (e) {}
function save() { try { localStorage.setItem(KEY, JSON.stringify(state)); } catch (e) {} }
function render() {
  let n = 0; const lines = [];
  for (const it of ITEMS) {
    const s = state[it.id] || {};
    document.querySelectorAll('.scheme[data-item="' + it.id + '"]').forEach(el => el.classList.toggle('on', s.pick === el.dataset.key));
    const none = document.querySelector('.none[data-item="' + it.id + '"]');
    none.classList.toggle('on', s.pick === 'none');
    const input = none.querySelector('input');
    if (document.activeElement !== input) input.value = s.note || '';
    const nav = document.querySelector('[data-nav="' + it.id + '"]');
    if (nav) nav.classList.toggle('done', !!s.pick);
    const note = (s.note || '').trim();
    let v;
    if (s.pick === 'none') v = '都不要' + (note ? '，我想要：' + note : '，我想要：____');
    else if (s.pick) v = '选 ' + s.pick + (note ? '（' + note + '）' : '');
    else v = '__（A/B/C/都不要：____）' + (note ? '　' + note : '');
    if (s.pick) n++;
    lines.push(it.id + ' ' + it.name + '：' + v);
  }
  document.getElementById('out').value = lines.join('\\n');
  document.getElementById('count').textContent = '已选 ' + n + ' / ' + ITEMS.length + ' 项';
}
document.addEventListener('click', e => {
  const b = e.target.closest('.pick'); if (!b) return;
  const sc = b.closest('.scheme'), none = b.closest('.none');
  const id = (sc || none).dataset.item, key = sc ? sc.dataset.key : 'none';
  const s = state[id] || (state[id] = {});
  s.pick = s.pick === key ? '' : key;
  save(); render();
});
document.addEventListener('input', e => {
  const none = e.target.closest('.none'); if (!none) return;
  const s = state[none.dataset.item] || (state[none.dataset.item] = {});
  s.note = e.target.value; save(); render();
});
document.getElementById('copy').addEventListener('click', async () => {
  const t = document.getElementById('out'); t.select();
  try { await navigator.clipboard.writeText(t.value); } catch (e) { document.execCommand('copy'); }
  const b = document.getElementById('copy'); b.textContent = '已复制'; setTimeout(() => b.textContent = '复制回复', 1500);
});
document.getElementById('clear').addEventListener('click', () => { if (confirm('清空所有选择？')) { state = {}; save(); render(); } });
render();
'''.replace('__META__', meta)

base_dir = os.path.join(root, '01-全局基调/1.1-配色/A-暖纸墨青')
base_cells = ''.join(cell(find(base_dir, f"{lv}-{LEVELS[lv][0]}"), f"第 {int(lv)} 关 · {LEVELS[lv][0]}") for lv in ('34', '33', '13'))
n_static = sum(len(it['schemes']) * len(it['levels']) for it in ITEMS)
n_gif = sum(len(it['schemes']) * len(it['films']) for it in ANIMS)

HTML = f'''<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Bloom 美术方案 · 审核报告</title><style>{CSS}</style></head><body><div class="wrap">
<nav><a href="#top">怎么审</a>{nav}<a href="#reply">回复</a></nav>
<main id="top">
<h1>Bloom 美术方案 · 审核报告</h1>
<p class="lead">2026-11-18。{len(all_items)} 项，每项 A / B / C 三个方案，共 {n_static} 张效果图、{n_gif} 张游戏内动图。
每一项点一下左边的字母就算选了（都不满意就点"都不要"写一句），页面最下面会自动拼好回复，复制发给我就行。选择会记在这台电脑的浏览器里，关掉再开还在。</p>

<div class="how">
<h3>按什么顺序审</h3>
<ol>
<li><b>先审一、全局基调</b>（配色 → 层次感 → 线条）。它是所有东西的底子，定了以后我会把后面的图按你选的重出一遍。</li>
<li><b>再审二、元素造型</b>。每一项只比一样东西，互相基本独立，可以跳着看。</li>
<li><b>然后三、动画</b>。动图会自己循环，建议每项先看 A（现在的样子）再看 B、C。</li>
<li><b>四、界面</b>放最后，它建立在前三类之上。</li>
</ol>
<h3 style="margin-top:14px">哪些项连在一起</h3>
<ul>
<li><b>2.4 火星 ↔ 3.5 火星飞行</b>、<b>2.6 门 ↔ 3.6 门开关</b>：是同一组设计的静态图和动图，两边填同一个字母就行。</li>
<li><b>1.3 线条 ↔ 2.5 墙和笼子</b>：1.3 管直角圆角和粗细，2.5 管实心空心。如果 1.3 选了 C（细线），2.5 就不用选了。</li>
<li><b>2.2 硬球 → 3.4 碎壳</b>：碎壳动画是照 2.2 的 A（同心壳）做的。2.2 选了别的，碎壳我会按新造型重做，3.4 你只要告诉我喜欢哪种"感觉"。</li>
<li><b>3.2 球的消失 → 3.7 过关</b>：过关的三版都是让满屏的余韵环动一下。3.2 没选 A（余韵环）也能用，只是那些环变成过关时才出现。</li>
<li><b>2.3 分裂球</b>的 B、C 靠球上的图案指方向，和 <b>2.7 辅助线</b>里的瞄准线是互相印证的，不冲突。</li>
</ul>
<h3 style="margin-top:14px">"暂定基准"是什么</h3>
<p>为了让你一次只比一样东西，每一项出图时别的维度都固定在同一套样子上，就是第四轮效果图 24–26 的样子：
暖纸白底 <code>#F7F4EE</code>、墨青球 <code>#465A58</code>、朱红着火 <code>#D9573F</code>、深墨圆角墙、每个形状下面 3 像素的错位影子、
蓝色逃跑球、带一圈壳的墨绿硬球、五彩色带的分裂球、点线瞄准线。它只是个参照，不代表已经定了。每项里标着"暂定基准"的那个方案就是它本身。</p>
<div class="basegrid">{base_cells}</div>
<h3 style="margin-top:14px">看图时请忽略的</h3>
<ul>
<li>效果图是 GPT 照着游戏截图画的，同一项的三个方案之间，球的大小、字的粗细偶尔有一点出入，那不是方案的一部分，只看这一项在比的东西。</li>
<li>动图是游戏里真跑的，墙和门是直角、没有影子，也不是方案的一部分，只看动作。</li>
<li>左上角的英文关名和暂停钮只是占位，界面在第四类里单独定。</li>
</ul>
<h3 style="margin-top:14px">没有拿来当方案的</h3>
<p>已经定死的规矩全部照守，没有哪个方案碰它们：浅色底；球一定是正圆；不要深色底加发光；普通球被引爆是膨胀一下就消失，没有火焰和爆炸；
显眼一点的效果只给了玩家点的那一下（3.1）；没有任何材质。</p>
<ul>{ui_note}<li>这次没做：五（无尽皮肤、商店、图标、itch 封面）、转场、声音。</li></ul>
</div>

{"".join(parts)}

<h2 id="reply">回复</h2>
<div class="replybox">
<p>上面点过的选择会自动填在这里，没选的留着空。可以直接在框里改，然后复制发给我。<span id="count"></span></p>
<textarea id="out" spellcheck="false">{esc(template)}</textarea>
<div><button class="btn" id="copy" type="button">复制回复</button><button class="btn ghost" id="clear" type="button">清空选择</button></div>
<p class="note">不想点的话，也可以直接照这个格式手写：<code>1.1 选 A、2.3 选 C、3.2 都不要，我想要……</code></p>
</div>
</main></div><script>{JS}</script></body></html>'''

open(os.path.join(root, '审核报告.html'), 'w', encoding='utf-8').write(HTML)
missing = HTML.count('还没出图')
print('report written:', len(all_items), 'items;', missing, 'images missing; UI sections:', len(ui_ready))
