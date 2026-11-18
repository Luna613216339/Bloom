#!/bin/zsh
# 用法：工具/出图.sh <清单.tsv 里的一行>     （在工程根目录运行）
# 一行 = 任务文件 <TAB> 参考图...；提示词从 stdin 喂给 Codex，参考图用 -i 传。
# 模型固定 gpt-6.1-sol + high（不要用 astra / ultra / max）。
line="$1"
parts=("${(@s:	:)line}")
task="${parts[1]}"
args=()
for r in "${parts[@]:1}"; do args+=(-i "$r"); done
log="${LOGDIR:-/tmp}/$(basename "$task" .txt).log"
/Applications/ChatGPT.app/Contents/Resources/codex-cli/bin/codex exec --enable image_generation -s workspace-write \
  -m gpt-6.1-sol -c model_reasoning_effort='"high"' "${args[@]}" < "$task" > "$log" 2>&1
echo "done $(basename "$task" .txt) exit=$?"
