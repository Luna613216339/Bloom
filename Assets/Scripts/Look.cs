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

    // ---- 动画方案（2026-11-18 美术方案审核用）----
    // 每一项有 A / B / C 三版，A 都是"现在的样子"。只改外观，不碰判定和时间。
    // 用户在 Docs/美术方案/审核报告.html 里选完以后，把选中的字母写成这里的默认值，其余两版可以删。
    // 命令行可以临时指定（给自动测试包抓动图用）：-look click=B,pop=C,skin=1

    /// <summary>3.1 玩家点出的圈：A 实心圆盘 / B 细环加落点 / C 三道涟漪</summary>
    public static FxV ClickFx = FxV.A;
    /// <summary>3.2 普通球膨胀和消失：A 实心盘、烧完留余韵环 / B 实心盘、从中心空掉 / C 空心环加小核</summary>
    public static FxV PopFx = FxV.A;
    /// <summary>3.3 逃跑球逃跑：A 拖尾 / B 先一惊、留一串点 / C 残影、停下喘口气</summary>
    public static FxV FleeFx = FxV.A;
    /// <summary>3.4 硬球碎壳：A 壳环散开、闪一秒 / B 裂成三段弧、倒计时环 / C 壳收紧、颜色由浅回深</summary>
    public static FxV CrackFx = FxV.A;
    /// <summary>3.5 火星：A 渐细尾巴 / B 省略号 / C 空心环加速度线</summary>
    public static FxV SparkFx = FxV.A;
    /// <summary>3.6 门（只在基准皮下生效）：A 伸缩门叶 / B 百叶 / C 转门</summary>
    public static FxV DoorFx = FxV.A;
    /// <summary>3.7 开场：A 直接出现 / B 沿队伍顺序长出来 / C 从中心一圈圈冒出来</summary>
    public static FxV IntroFx = FxV.A;
    /// <summary>3.7 过关：A 余韵环从中心依次亮 / B 一道大环扫过、把环带走 / C 满屏的环一起开成花</summary>
    public static FxV WinFx = FxV.A;
    /// <summary>3.7 失败：A 余韵安静淡掉 / B 没收到的球褪色 / C 没收到的球被圈出来</summary>
    public static FxV FailFx = FxV.A;

    /// <summary>
    /// 暂定基准皮：暖纸白底、墨青球、朱红着火、深墨墙，门画成两片门叶。
    /// 默认关（游戏还是白模），抓动图时用 -look skin=1 打开。用户定了配色以后改这里。
    /// </summary>
    public static bool Skin = false;

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
            FxV fx = v == "B" ? FxV.B : v == "C" ? FxV.C : FxV.A;
            switch (k)
            {
                case "click": ClickFx = fx; break;
                case "pop": PopFx = fx; break;
                case "flee": FleeFx = fx; break;
                case "crack": CrackFx = fx; break;
                case "spark": SparkFx = fx; break;
                case "door": DoorFx = fx; break;
                case "intro": IntroFx = fx; break;
                case "win": WinFx = fx; break;
                case "fail": FailFx = fx; break;
            }
        }
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    /// <summary>基准皮是不是在用（白模开着、不是无尽模式、皮开着）</summary>
    public static bool Skinned => Skin && On;

    /// <summary>正式关卡的底色</summary>
    public static Color Backdrop => Skin && Whitebox ? Hex("#F7F4EE") : Color.white;

    /// <summary>"着火"的强调色</summary>
    public static Color Fire => Skinned ? Hex("#D9573F") : BallPalette.Accent;

    /// <summary>普通球的颜色（白模 / 基准皮下只有一种）</summary>
    public static Color Ink => Skinned ? Hex("#465A58") : BallPalette.Whitebox[0];

    static readonly Color[] skinPalette = { Hex("#465A58") };

    /// <summary>球烧完以后实心部分淡掉用的时间（原来是缩小，约 0.37 秒）</summary>
    public const float VanishTime = 0.3f;

    /// <summary>瞄准虚线的颜色。朱红留给"着火"，虚线用灰</summary>
    public static Color Aim => Skinned ? new Color(0.62f, 0.69f, 0.65f, 0.95f) : new Color(0.42f, 0.42f, 0.42f, 0.55f);

    static bool Story => GameManager.Instance == null || !GameManager.Instance.IsEndless;

    /// <summary>此刻该不该按白模画：开关开着，而且不是无尽模式</summary>
    public static bool On => Whitebox && Story;

    /// <summary>普通球的配色。白模里所有普通球同一种中灰</summary>
    public static Color[] Palette(Color[] authored) => On ? (Skin ? skinPalette : BallPalette.Whitebox) : authored;

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
    public static Color Escape(Color authored) => Skinned ? Hex("#4A7FB0") : On ? BallPalette.WhiteboxEscape : authored;

    /// <summary>硬球</summary>
    public static Color Hard(Color authored) => Skinned ? Hex("#2F6B57") : On ? BallPalette.WhiteboxHard : authored;

    /// <summary>墙、笼子</summary>
    public static Color Structure(Color authored) => Skinned ? Hex("#37433F") : On ? BallPalette.WhiteboxStructure : authored;

    /// <summary>门。白模里普通门和联动门颜色一样，靠花纹分（见 SpriteHelper.DoorClosed / DoorLinked）</summary>
    public static Color Door(bool linked, Color authored) =>
        Skinned ? (linked ? Hex("#C9983E") : Hex("#7C8985")) : On ? BallPalette.WhiteboxStructure : authored;

    /// <summary>蛇的路径线</summary>
    public static Color Path(Color authored) => On ? new Color(0.5f, 0.5f, 0.5f, 0.22f) : authored;
}

/// <summary>动画方案的三个版本。A 一律是加方案之前的样子</summary>
public enum FxV { A, B, C
}
