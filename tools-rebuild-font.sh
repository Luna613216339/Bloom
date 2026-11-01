#!/bin/bash
# 重新生成中文子集字体。加了新中文文案之后跑这个，否则新字会显示成空白。
#
# 前置：
#   1. Unity 菜单 Tools → Bloom → Export Font Charset  （生成 FontCharset.txt）
#   2. 完整的 Noto Sans SC 放在 ThirdParty/Fonts/（17MB，不进 git）；没有的话脚本自己从 Google Fonts 官方仓库下载
#
# 用法：  bash tools-rebuild-font.sh            用 ThirdParty/Fonts 里那份
#         bash tools-rebuild-font.sh <字体路径>  换别的字体

set -e
cd "$(dirname "$0")"
FULL=ThirdParty/Fonts/NotoSansSC-wght.ttf
if [ -z "$1" ] && [ ! -f "$FULL" ]; then
  echo "没有完整字体，从 Google Fonts 下载（约 17MB）"
  mkdir -p ThirdParty/Fonts
  curl -fSL -o "$FULL" "https://github.com/google/fonts/raw/main/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf"
fi
SRC="${1:-$FULL}"

# 可变字体先固定到 Regular，Unity 的 legacy Font 不吃可变字体
if python3 -c "from fontTools.ttLib import TTFont; exit(0 if 'fvar' in TTFont('$SRC') else 1)" 2>/dev/null; then
  echo "检测到可变字体，固定 wght=400"
  python3 -m fontTools.varLib.instancer "$SRC" wght=400 -o /tmp/_bloom_static.ttf
  SRC=/tmp/_bloom_static.ttf
fi

# 中文字符集 + ASCII + 界面符号
python3 - <<'PY'
cjk = open('FontCharset.txt', encoding='utf-8-sig').read()
ascii_ = ''.join(chr(c) for c in range(0x20, 0x7F))
extra = '·…—◆●○✓'
open('/tmp/_bloom_chars.txt','w',encoding='utf-8').write(''.join(sorted(set(cjk+ascii_+extra))))
PY

pyftsubset "$SRC" \
  --text-file=/tmp/_bloom_chars.txt \
  --output-file=Assets/Resources/Fonts/UIFont.ttf \
  --layout-features='' --no-hinting --desubroutinize \
  --drop-tables+=DSIG --name-IDs=''

echo "完成： $(ls -lh Assets/Resources/Fonts/UIFont.ttf | awk '{print $5}')"
