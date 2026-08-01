using UnityEngine;

/// <summary>
/// 每关一套球的配色。改配色只改这个文件，场景和 Inspector 都不用动。
///
/// 两种取色方式：
/// - Cycle：散布关（1-5）用，颜色是"标签"，按索引循环，让相邻的球分得开
/// - Ramp：蛇形关（6-10）用，颜色是"蛇的身体"，沿队列插值成连续渐变，
///         短蛇（第 8 关每条只有 4 颗彩球）也能吃到完整色域
/// </summary>
public static class BallPalette
{
    static Color C(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    // 朦胧夏日：橙 → 黄 → 黄绿 → 青绿
    static readonly Color[] HazySummer =
    {
        C("#EFB055"), C("#E3CC5A"), C("#9DC454"), C("#74BD63"), C("#55B287"),
    };

    // 涩谷夜：琥珀 → 品红 → 紫 → 电蓝 → 青。琥珀放队首，渐变时不会经过灰绿
    static readonly Color[] ShibuyaNight =
    {
        C("#F0A128"), C("#FF1F6B"), C("#F73FA0"), C("#C13AFF"),
        C("#6B4BFF"), C("#2D9CFF"), C("#00BFD8"),
    };

    // 马卡龙糖果
    static readonly Color[] Macaron =
    {
        C("#F5A0B4"), C("#B9E08D"), C("#8FD3E8"), C("#B4A6E5"), C("#F2AED6"), C("#F8D982"),
    };

    // 靛蓝 → 蒂芙尼蓝。原来是单色靛蓝的明暗渐变，但单色在白底上必然失败：
    // 一个色相只能靠明度分层，往亮走就逼近白色，最浅的几颗直接看不见。
    // 混进第二个色相（青绿）之后，每一档都能保住饱和度，最亮端也立得住。
    static readonly Color[] Tiffany =
    {
        C("#3E4EB4"), C("#3D7BD4"), C("#2E96CE"), C("#1FAEC4"), C("#4BC9BD"),
    };

    // 双色对撞 · 青 × 橙。冷暖交替排列，散布时对比最强
    static readonly Color[] TealOrange =
    {
        C("#EE7B30"), C("#F4A259"), C("#5FCBC4"), C("#E15A1D"), C("#127F8A"),
    };

    // 樱花藕粉
    static readonly Color[] Sakura =
    {
        C("#F2A88F"), C("#F49BAE"), C("#E370AA"), C("#D081C4"), C("#A79BE0"),
    };

    // 涩谷夜的深色版。不是"同一套色换个背景"—— 白底靠暗端立住的颜色，
    // 到黑底上会直接沉进背景。整条色阶的明度都往上抬了一档，
    // 尤其是原来那颗靛蓝 #6B4BFF，在黑底上几乎看不见
    static readonly Color[] ShibuyaNightDark =
    {
        C("#FFB347"), C("#FF4D7D"), C("#FF5CB0"), C("#CE6BFF"),
        C("#8A7BFF"), C("#4FB4FF"), C("#2EE0F0"),
    };

    // ---- 2026-08-01 新增。全部按"爆炸态对比度"筛过：白底 ≥1.30，深色底 ≥3.0。
    //      基准锚是实机观感 —— 涩谷夜 1.41 好看，马卡龙 1.16 太浅 ----



    // 翡翠林：绿为主角。色相图上最大的一块空白
    static readonly Color[] Emerald =
    {
        C("#075E3F"), C("#0E8055"), C("#17A05B"), C("#4CB33C"),
        C("#8CC220"), C("#0FA588"), C("#046B62"),
    };




    // 极光（深色）：薄荷 → 青绿 → 靛 → 紫，一条连续冷色渐变
    static readonly Color[] Aurora =
    {
        C("#7CFFB2"), C("#3FE8B0"), C("#23D6C8"), C("#2FB4E8"),
        C("#5B8DF0"), C("#916DF0"), C("#C56BE0"),
    };

    // 午夜植物园（深色）：酸性绿打头，底色是墨绿黑。深色方案里对比度最高（3.53）
    static readonly Color[] MidnightGarden =
    {
        C("#9EF01A"), C("#55D66B"), C("#2ED8A0"), C("#4FE0D0"),
        C("#FFD046"), C("#FF8C42"), C("#FF5B7F"),
    };

    // 地狱火（深色）：烬灰 → 火橙 → 朱红 → 血红，近黑带红的底。
    //
    // 只有 4 色，不是凑不够 —— 是加不了。能再加的只剩更暗的红，
    // 而暗红在黑底上必然沉底（试过一颗绛红 #D6304F，整套对比度掉到 2.56）。
    // 四色是这个主题在深色底上的天花板，不是妥协。
    //
    // 原来还有一颗骨白当火心，去掉了 —— 太亮，在这套里像个异物。
    // 现在最亮的是烬灰，整套压在暖红区间内，邪气才出得来。
    static readonly Color[] Hellfire =
    {
        C("#9E8B86"), C("#FF7A2E"), C("#FF442E"), C("#FF2148"),
    };

    // 糖霜（深色）：粉彩上黑底。
    // 来历有点意思 —— 粉彩在白底上必然失败（马卡龙就是这么死的，爆炸态对比度 1.16），
    // 但在黑底上它是最强的一档，因为全是高明度。同一批颜色换个底就从最差变最好
    static readonly Color[] Frosting =
    {
        C("#FFA8C4"), C("#FFCB9E"), C("#FBEF9E"), C("#9CE8C2"),
        C("#9AD4FF"), C("#B7A6F5"), C("#F0A6DC"),
    };

    // 原版彩虹，只留给第 10 关
    static readonly Color[] Rainbow =
    {
        C("#FF5959"), C("#FF9933"), C("#FFE64D"), C("#66FF80"),
        C("#4DCCFF"), C("#B380FF"), C("#FF80CC"),
    };

    // 索引 = 关卡号，0 位空着不用
    static readonly Color[][] ByLevel =
    {
        null,
        HazySummer,     // 1  散布 20 颗
        ShibuyaNight,   // 2  散布 28 颗
        Tiffany,        // 3  散布 40 颗
        Macaron,        // 4  散布 55 颗
        TealOrange,     // 5  散布 60 颗
        ShibuyaNight,   // 6  单条蛇 8 颗
        HazySummer,     // 7  单条蛇 10 颗（3 颗逃跑球）
        HazySummer,     // 8  双蛇各 6 颗（各 2 颗逃跑球）
        Sakura,         // 9  双圆弧各 9 颗
        Rainbow,        // 10 五圆弧各 8 颗
    };

    /// <summary>关卡号（1 起）取配色。加关卡时只改上面的 ByLevel 表</summary>
    public static Color[] ForLevel(int level)
    {
        return ByLevel[Mathf.Clamp(level, 1, ByLevel.Length - 1)];
    }

    // ---- 商店主题。只在无尽模式生效，正式关卡永远用 ByLevel 表 ----

    // 0 号是免费初始主题，玩家一进商店就已经装备着它。
    // 排列顺序：先白底后深色，深色排最后 —— 商店从左上往右下扫过去，
    // 观感是"由浅入深"，最后那三张黑卡自成一组，不会和白底卡混在一起。
    //
    // ⚠️ 改动这几个数组会打乱存档索引（存档存的是索引号，不是名字）。
    // ProgressManager 有个 ThemeSchema 版本号，改完记得 +1，
    // 否则老玩家会"拥有"错位的主题。
    static readonly Color[][] ThemePalettes =
    {
        HazySummer, Emerald, Sakura, Tiffany, ShibuyaNight, Rainbow,
        ShibuyaNightDark, Aurora, MidnightGarden, Hellfire, Frosting,
    };

    public static readonly string[] ThemeNames =
    {
        "Hazy Summer", "Emerald", "Sakura", "Tiffany", "Shibuya Night", "Rainbow",
        "Shibuya Dark", "Aurora", "Midnight Garden", "Hellfire", "Frosting",
    };

    /// <summary>主题名的中文版，索引和 ThemeNames 一一对应。取名走 Loc.ThemeName(i)</summary>
    public static readonly string[] ThemeNamesZh =
    {
        "朦胧夏日", "翡翠林", "樱花藕粉", "蒂芙尼", "涩谷夜", "原版彩虹",
        "涩谷夜·暗", "极光", "午夜植物园", "地狱火", "糖霜",
    };

    /// <summary>按当前语言取主题名</summary>
    public static string ThemeName(int index)
    {
        int i = Clamp(index);
        return Loc.IsZh ? ThemeNamesZh[i] : ThemeNames[i];
    }


    /// <summary>
    /// 深色主题定在 300：换深色不是换配色，是换掉整个游戏的观感，价值不是一个档次。
    /// 但涩谷夜·深色版只要 50 —— 它是**深色这个品类的入场券**：
    /// 便宜到几乎不构成决策，玩家买了才知道原来还能这样，
    /// 另外两套 300 的深色才有人考虑。
    /// 刻意不做成免费：白送的东西玩家不会认真看，花 50 买的才会去用。
    ///
    /// 50 同时也是全场最低价，为了让第一次购买发生在第一局之内 ——
    /// 商店如果第一局摸不到，玩家就当它是装饰，之后不会再点。
    /// </summary>
    public static readonly int[] ThemePrices =
    {
        0, 50, 100, 100, 150, 300,
        50, 300, 300, 200, 150,
    };

    /// <summary>
    /// 每个主题的背景色。白底是这个游戏的默认，深色主题各自带自己的底。
    ///
    /// 深色底不用纯黑：纯黑配霓虹会产生刺眼的振动感，而且任何 UI 接缝都会露出来。
    /// 抬一点、往紫偏一点，才读得出"夜"而不是"关掉的屏幕"。
    /// </summary>
    static readonly Color[] ThemeBackgrounds =
    {
        Color.white, Color.white, Color.white, Color.white, Color.white, Color.white,
        C("#15121E"),   // 涩谷夜·深色版：近黑的紫调
        C("#0A1024"),   // 极光：近黑的夜蓝
        C("#0D1512"),   // 午夜植物园：墨绿黑
        C("#0C0304"),   // 地狱火：近纯黑，带一点红
        C("#16131C"),   // 糖霜：近黑微紫
    };

    public static int ThemeCount => ThemePalettes.Length;

    static int Clamp(int index) => Mathf.Clamp(index, 0, ThemePalettes.Length - 1);

    public static Color[] ForTheme(int index)
    {
        return ThemePalettes[Clamp(index)];
    }

    public static Color BackgroundForTheme(int index)
    {
        return ThemeBackgrounds[Clamp(index)];
    }

    /// <summary>
    /// 由背景亮度算，不另外维护一张"哪些是深色"的表 —— 两张表迟早会对不上。
    /// </summary>
    public static bool IsDarkTheme(int index)
    {
        var bg = BackgroundForTheme(index);
        return bg.r * 0.299f + bg.g * 0.587f + bg.b * 0.114f < 0.5f;
    }

    /// <summary>HUD 主文字色（分数那种），跟着背景明暗翻转</summary>
    public static Color InkFor(bool dark)
    {
        return dark ? new Color(0.93f, 0.93f, 0.96f) : new Color(0.3f, 0.3f, 0.3f);
    }

    /// <summary>HUD 次要文字色（关卡名那种）</summary>
    public static Color MutedInkFor(bool dark)
    {
        return dark ? new Color(0.62f, 0.62f, 0.72f) : new Color(0.55f, 0.55f, 0.55f);
    }

    /// <summary>散布关用：颜色当标签，循环取</summary>
    public static Color Cycle(Color[] palette, int index)
    {
        return palette[index % palette.Length];
    }

    /// <summary>蛇形关用：把整条色阶压缩到 count 颗球上，插值取</summary>
    public static Color Ramp(Color[] palette, int index, int count)
    {
        if (count <= 1) return palette[0];

        float t = (float)index / (count - 1) * (palette.Length - 1);
        int i = Mathf.FloorToInt(t);
        if (i >= palette.Length - 1) return palette[palette.Length - 1];

        return Color.Lerp(palette[i], palette[i + 1], t - i);
    }
}
