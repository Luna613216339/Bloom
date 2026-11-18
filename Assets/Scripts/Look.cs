using UnityEngine;

/// <summary>
/// 外观层：正式关卡里每一样东西长什么样，都从这里取。玩法代码只问"这颗球现在该是什么颜色"，
/// 不自己决定。以后换美术风格只改这一个文件（和 BallPalette 里的色表）。
///
/// 现在是白模：白底，普通球一种中灰，结构深灰；强调色朱红表示"着火 / 在扩散"。
/// 特殊球带颜色：逃跑球蓝、硬球墨绿、分裂球和火星五彩（用户定的）。无尽模式和商店皮肤不走这里，照旧用主题配色。
/// </summary>
public static class Look
{
    /// <summary>白模开关。关掉就回到每关各自的作者配色</summary>
    public static bool Whitebox = true;

    /// <summary>
    /// 定向分裂球的瞄准虚线：从五彩球画出两条点线，照着火星真正会飞的路线描（碰到关着的门和墙会折）。
    /// 关掉就回到"看不见火星往哪飞"的旧样子，关卡照样能过。2026-11-10 Fable 定，要用户试玩确认。
    /// </summary>
    public static bool AimLines = true;

    /// <summary>
    /// 余韵：球烧完不再缩成一个点，而是实心淡掉、留一圈细环；过关时满屏的环依次亮一下散掉。
    /// 只是外观，判定和时间都不变。2026-11-18 加，等用户看。
    /// </summary>
    public static bool AfterglowEnabled = true;
    public static bool Afterglow => SoftVanish && PopFx == FxV.A;
    /// <summary>球烧完是原地淡掉（新）还是缩成一点（旧）。动画方案的 B、C 版都建立在"原地淡掉"上</summary>
    public static bool SoftVanish => AfterglowEnabled && On;

    // ---- 动画方案（美术方案审核用）----
    // 只改外观，不碰判定和时间。A 一律是加方案之前的样子（也是默认值，除非下面写了"已定"）。
    // 第一轮（2026-11-18）没被选中的版本已经删掉；D / E / F 是第二轮按反馈新做的版本，
    // 在第二轮审核页里显示成 A / B / C，对应关系见 Docs/美术方案/第二轮/工具/round2.py。
    // 命令行可以临时指定（给自动测试包抓动图用）：-look skin=1,click=D,pop=E

    /// <summary>3.1 玩家点出的圈：A 实心圆盘（现状） / D 圆盘里两道涟漪 / E 圆盘里一圈圈不断的涟漪 / F 只有涟漪、边是虚的</summary>
    public static FxV ClickFx = FxV.A;
    /// <summary>3.2 普通球膨胀和消失：A 实心盘、烧完留余韵环（现状） / B 从中心空掉 / D 空掉的同时变淡 / E 空到一半就散 / F 只淡出、不留线</summary>
    public static FxV PopFx = FxV.A;
    /// <summary>3.4 硬球掉一层壳：A 壳环散开（现状） / D 碎石四溅 / E 碎石往下掉 / F 碎石顺着火飞出去</summary>
    public static FxV CrackFx = FxV.A;
    /// <summary>3.6 门（只在基准皮下生效）：A 电梯门（两片对开） / D 伸缩门（一片缩进一侧） / E 闸刀门（一头有轴，扳开 90°）</summary>
    public static FxV DoorFx = FxV.A;
    /// <summary>3.7 开场：A 直接出现 / B 沿队伍顺序长出来 / C 从中心一圈圈冒出来。用户定：按关灵活用，表在第二轮审核页</summary>
    public static FxV IntroFx = FxV.A;
    /// <summary>3.7 过关：A 余韵环从中心依次亮（现状） / B 一道大环扫过 / D 细而淡的环扫过 / E 不画大环、只有一圈圈依次亮 / F 一道很宽很淡的光带扫过</summary>
    public static FxV WinFx = FxV.A;
    /// <summary>3.7 失败：A 余韵安静淡掉 / C 没收到的球被圈出来。**用户 2026-11-18 定 C**</summary>
    public static FxV FailFx = FxV.C;

    // ---- 硬球（2026-11-18 第二轮：规则原型 + 造型）----

    /// <summary>
    /// 硬球规则。Legacy 是正式规则（碎壳后 1 秒内的火不算），其余三个是给用户选的原型，**用户选定之前默认不动**。
    /// 命令行：-look hard=count|flame|sides
    /// </summary>
    public static HardRuleKind HardRule = HardRuleKind.Legacy;
    /// <summary>2.2 硬球造型（用了原型规则或基准皮时生效）：A 厚壳 / B 棱角壳 / C 甲片</summary>
    public static FxV HardLook = FxV.A;
    /// <summary>临时把所有硬球改成几层（0 = 按关卡写的）。命令行：-look layers=3</summary>
    public static int HardLayersOverride = 0;
    /// <summary>「两面夹」规则下壳缝的方向（度）。90 = 竖缝（左右两片），0 = 横缝（上下两片）。命令行：-look seam=0</summary>
    public static float HardSeamDeg = 90f;

    /// <summary>这颗硬球一共要挨几下才炸。Legacy 永远是 2</summary>
    public static int HardHits(string levelId, int authored)
    {
        if (HardRule == HardRuleKind.Legacy || HardRule == HardRuleKind.Sides) return 2;
        if (HardLayersOverride > 0) return HardLayersOverride;
        if (authored > 0) return authored;
        // 「数层数」规则下硬壳这关是三层：点出的圈一下、旁边的球一下、绕一圈回来的火一下（用户的提议）
        if (HardRule == HardRuleKind.Count && levelId == "shell") return 3;
        return 2;
    }

    /// <summary>硬球是不是由 HardShell 另外画壳（原型规则和基准皮都是）。否则还是贴图里那圈透明缝</summary>
    public static bool HardOverlay => On && (Skin || HardRule != HardRuleKind.Legacy);

    /// <summary>
    /// 基准皮：纸色底、一种墨色的球、朱红着火、同色的墙，门画成门叶；带错位影子、逃跑球的白点、对半的分裂球（用户 11-18 定的那几项）。
    /// 默认关（游戏还是白模），抓图时用 -look skin=1 打开。Pal 选哪套颜色。
    /// </summary>
    public static bool Skin = false;
    /// <summary>基准皮的配色：0 = 第一轮的暖纸墨青；1 / 2 / 3 = 第二轮 1.1 的三个变体（淡蓝底 + 墨黑球，鲜明程度递增）</summary>
    public static int Pal = 2;

    struct Tone { public Color bg, ink, fire, wall, door, linked, escape, hard, shadow, aim; }
    static Tone T(string bg, string ink, string fire, string wall, string door, string linked, string escape, string hard, string shadow, string aim) =>
        new Tone { bg = Hex(bg), ink = Hex(ink), fire = Hex(fire), wall = Hex(wall), door = Hex(door), linked = Hex(linked),
                   escape = Hex(escape), hard = Hex(hard), shadow = Hex(shadow), aim = Hex(aim) };
    static readonly Tone[] tones =
    {
        T("#F7F4EE", "#465A58", "#D9573F", "#37433F", "#7C8985", "#C9983E", "#4A7FB0", "#2F6B57", "#E7E1D6", "#B9C4BD"),
        // 1：第一轮 C 的底色原样 + 第一轮 B 的颜色原样
        T("#EEF2F4", "#26282B", "#F04E2A", "#26282B", "#7D8792", "#D99A2B", "#2F7DE1", "#138A72", "#DCE3E8", "#B4BEC6"),
        // 2：底色蓝得明确一点，墨色带一点蓝
        T("#E2ECF3", "#1E2630", "#F2472A", "#1E2630", "#6F7F90", "#E09A1F", "#2374E0", "#0E8F74", "#CBD9E4", "#A3B4C2"),
        // 3：最鲜明，底色是看得出来的天蓝，球是深海军蓝，火更亮
        T("#D3E6F5", "#14213A", "#FF4420", "#14213A", "#5F7896", "#F0A000", "#1667E6", "#00997A", "#B7D2E8", "#8FAAC4"),
    };
    static Tone Now => tones[Mathf.Clamp(Pal, 0, tones.Length - 1)];

    static Look()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-look") Parse(args[i + 1]);
    }

    public static void Parse(string spec)
    {
        foreach (var kv in spec.Split(','))
        {
            var p = kv.Split('=');
            if (p.Length != 2) continue;
            string k = p[0].Trim().ToLowerInvariant(), v = p[1].Trim().ToUpperInvariant();
            if (k == "skin") { Skin = v == "1" || v == "TRUE"; continue; }
            if (k == "aim") { AimLines = v == "1" || v == "TRUE"; continue; }
            if (k == "pal") { int.TryParse(v, out Pal); continue; }
            if (k == "layers") { int.TryParse(v, out HardLayersOverride); continue; }
            if (k == "seam") { float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out HardSeamDeg); continue; }
            if (k == "hard")
            {
                HardRule = v == "COUNT" ? HardRuleKind.Count : v == "FLAME" ? HardRuleKind.Flame
                         : v == "SIDES" ? HardRuleKind.Sides : HardRuleKind.Legacy;
                continue;
            }
            FxV fx = v.Length == 1 && v[0] >= 'A' && v[0] <= 'F' ? (FxV)(v[0] - 'A') : FxV.A;
            switch (k)
            {
                case "click": ClickFx = fx; break;
                case "pop": PopFx = fx; break;
                case "crack": CrackFx = fx; break;
                case "door": DoorFx = fx; break;
                case "intro": IntroFx = fx; break;
                case "win": WinFx = fx; break;
                case "fail": FailFx = fx; break;
                case "hardlook": HardLook = fx; break;
            }
        }
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    /// <summary>基准皮是不是在用（白模开着、不是无尽模式、皮开着）</summary>
    public static bool Skinned => Skin && On;

    /// <summary>正式关卡的底色</summary>
    public static Color Backdrop => Skin && Whitebox ? Now.bg : Color.white;

    /// <summary>"着火"的强调色</summary>
    public static Color Fire => Skinned ? Now.fire : BallPalette.Accent;

    /// <summary>普通球的颜色（白模 / 基准皮下只有一种）</summary>
    public static Color Ink => Skinned ? Now.ink : BallPalette.Whitebox[0];

    /// <summary>错位影子的颜色（基准皮才有影子）</summary>
    public static Color Shadow => Now.shadow;
    /// <summary>影子往下错开多少（世界单位，约 3 像素）</summary>
    public const float ShadowDrop = 0.05f;

    static readonly Color[] skinPalette = new Color[1];

    /// <summary>球烧完以后实心部分淡掉用的时间（原来是缩小，约 0.37 秒）</summary>
    public const float VanishTime = 0.3f;

    /// <summary>瞄准虚线的颜色。朱红留给"着火"，虚线用灰</summary>
    public static Color Aim => Skinned ? Now.aim : new Color(0.42f, 0.42f, 0.42f, 0.55f);

    static bool Story => GameManager.Instance == null || !GameManager.Instance.IsEndless;

    /// <summary>此刻该不该按白模画：开关开着，而且不是无尽模式</summary>
    public static bool On => Whitebox && Story;

    /// <summary>普通球的配色。白模里所有普通球同一种中灰</summary>
    public static Color[] Palette(Color[] authored)
    {
        if (!On) return authored;
        if (!Skin) return BallPalette.Whitebox;
        skinPalette[0] = Now.ink;
        return skinPalette;
    }

    /// <summary>球被点着、正在膨胀时的颜色</summary>
    public static Color Burn(Color original, bool darkBackground)
    {
        if (On)
        {
            var a = Fire;
            return new Color(a.r, a.g, a.b, 0.72f);
        }
        // 半透明在白底上是"变浅"，在深色底上是"变暗"——后者会把霓虹球的光感抽掉，深色留多一点不透明度
        return new Color(original.r, original.g, original.b, darkBackground ? 0.7f : 0.45f);
    }

    /// <summary>玩家点出来的那个圈：它也是"火"，白模里用同一个强调色</summary>
    public static Color ClickRing(bool darkBackground)
    {
        if (On)
        {
            var a = Fire;
            return new Color(a.r, a.g, a.b, 0.38f);
        }
        float g = darkBackground ? 0.85f : 0.6f;
        return new Color(g, g, g, 0.45f);
    }

    /// <summary>火星的拖尾</summary>
    public static Color SparkTrail => On ? Fire : new Color(1f, 0.6f, 0.2f);

    /// <summary>逃跑球。贴图本身是明暗条纹，这里给的是底色</summary>
    public static Color Escape(Color authored) => Skinned ? Now.escape : On ? BallPalette.WhiteboxEscape : authored;

    /// <summary>硬球</summary>
    public static Color Hard(Color authored) => Skinned ? Now.hard : On ? BallPalette.WhiteboxHard : authored;

    /// <summary>墙、笼子</summary>
    public static Color Structure(Color authored) => Skinned ? Now.wall : On ? BallPalette.WhiteboxStructure : authored;

    /// <summary>门。白模里普通门和联动门颜色一样，靠花纹分（见 SpriteHelper.DoorClosed / DoorLinked）</summary>
    public static Color Door(bool linked, Color authored) =>
        Skinned ? (linked ? Now.linked : Now.door) : On ? BallPalette.WhiteboxStructure : authored;

    /// <summary>蛇的路径线</summary>
    public static Color Path(Color authored) => On ? new Color(0.5f, 0.5f, 0.5f, 0.22f) : authored;
}

/// <summary>动画方案的三个版本。A 一律是加方案之前的样子</summary>
public enum FxV { A, B, C, D, E, F }

/// <summary>硬球规则。Legacy = 正式规则；其余是 2026-11-18 第二轮给用户选的原型</summary>
public enum HardRuleKind
{
    /// <summary>现状：第一把火打掉壳，之后 1 秒内的火不算，再来一把才炸</summary>
    Legacy,
    /// <summary>数层数：每一颗碰到它的着火球 / 点出的圈 / 火星各打掉一层（同一颗只算一次），打完就炸</summary>
    Count,
    /// <summary>一场火一层：被火围着的时候只掉一层；身边的火全灭了，再来的火才掉下一层</summary>
    Flame,
    /// <summary>两面夹：壳分两片，火从哪一侧来就掉哪一片，两片都掉了才炸</summary>
    Sides,
}
