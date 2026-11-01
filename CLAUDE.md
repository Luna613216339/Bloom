# Bloom · 给 AI 的入口

Bloom 是一个 2D 连锁反应小游戏（Unity 6000.3.19f1，C#，OnGUI 做界面）。
点一下，扩散圈碰到的球膨胀、再去碰下一颗；门可以在连锁进行中开关。
itch.io 上有旧版可玩：luna25-7430528.itch.io/bloom

## 先读什么

1. `Docs/交接.md`：现在在哪一步、下一步做什么。**每次开工先读它，收工前更新它。**
2. `Docs/任务说明-第二阶段.md`：当前这一阶段的任务（白模 + 大量备选关）；体验目标、工作规矩和交付格式沿用 `Docs/任务说明-关卡扩展.md`
3. `Docs/决策记录.md`：已经定下来的设计决定和理由，别推翻，要改先问用户

更早的设计文档和踩坑记录在工程外的备份文件夹里，**只读，不要改**：
`~/Desktop/游戏开发资料/Bloom 备份（含文档）/开发笔记.md`（架构、已知坑 1–24、配色硬指标），
同目录还有 `Bloom 是怎么做出来的.md`（设计叙事）、`没做完的事.md`（旧待办，部分已过时）。
那个文件夹里的工程副本是旧的，别在里面改东西。

## 分工

用户是设计师：定体验、定验收标准、试玩、做设计判断。不看代码，也不 debug。
AI 负责实现、自己验证、管 Git。纯技术选择自己定，**不要拿技术问题问用户**。
只有影响玩法、手感、界面体验、美术方向、范围的事才停下来问。

## Git

- 这轮在 `level-expansion` 分支上做，**不要直接改 `main`，不要合并到 `main`**，合并由用户点头。
- 每完成一件能一句话说清的事就提交，不要攒。提交信息用中文，正文写为什么。
- 提交和 PR 里**不要出现 Claude / AI 的署名**（不加 `Co-Authored-By`），作者只有用户自己。
- 可以推送工作分支到 GitHub。不要 force push `main`。
- 大改之前先留一个能回退的提交。
- **不要发布到 itch.io**，除非用户明说。

## 技术上的坑（详见备份里的开发笔记）

- 改了 `[SerializeField]` 的默认值，对已经在 Inspector 里存过值的场景**无效**；要改就改场景 YAML 或 Inspector。
- Unity 6 没有 `UnityEngine.UI`，界面一律 OnGUI。物理 API 用 `linearVelocity` / `linearDamping`。
- **绝不赋值 `GUI.skin` 的任何字段**，会把编辑器字体一起弄坏。GUIStyle 一律经过 `Loc.Fit()`，运行时改字色用 `Loc.SetTextColor()`。
- 新加中文文案后要重新生成子集字体（`tools-rebuild-font.sh`），否则新字显示成空白或别的字。
- 不要改 `productName` / `companyName`，会让所有存档消失。
- `Editor/ChainSimulation.cs` 是游戏规则的数学副本，改了 `Ball.cs` / `ClickReaction.cs` 的判定要同步改它。
- 正式关卡永远白底；配色只从 `BallPalette.cs` 取。
- 编译验证：Unity 编辑器**关着**的时候可以用命令行
  `/Applications/Unity/Hub/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath <工程> -logFile -`。
  编辑器开着时工程被锁（`Temp/UnityLockfile`），这时用 Unity MCP（`Unity_RunCommand` 刷新编译、打包，`Unity_Camera_Capture` 截图，`Unity_GetConsoleLogs` 看控制台）；MCP 也连不上再请用户关掉 Unity。

## 对外措辞

不提早期参考过的任何作品，也不用「原版玩法」「复刻」这类暗示有原作的说法。无尽模式统一叫「第一版原型」。
