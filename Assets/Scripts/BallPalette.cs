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
        C("#EFB055"), C("#F0C65C"), C("#E3CC5A"), C("#C3C851"),
        C("#9DC454"), C("#74BD63"), C("#55B287"),
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
        C("#F5A0B4"), C("#F7CE86"), C("#B9E08D"), C("#8FD3E8"),
        C("#B4A6E5"), C("#F2AED6"), C("#F8D982"),
    };

    // 靛蓝 → 蒂芙尼蓝。原来是单色靛蓝的明暗渐变，但单色在白底上必然失败：
    // 一个色相只能靠明度分层，往亮走就逼近白色，最浅的几颗直接看不见。
    // 混进第二个色相（青绿）之后，每一档都能保住饱和度，最亮端也立得住。
    static readonly Color[] Tiffany =
    {
        C("#3E4EB4"), C("#4560C9"), C("#3D7BD4"), C("#2E96CE"),
        C("#1FAEC4"), C("#0ABAB5"), C("#4BC9BD"),
    };

    // 双色对撞 · 青 × 橙。冷暖交替排列，散布时对比最强
    static readonly Color[] TealOrange =
    {
        C("#0E9AA7"), C("#EE7B30"), C("#25B4B8"), C("#F4A259"),
        C("#5FCBC4"), C("#E15A1D"), C("#127F8A"),
    };

    // 樱花藕粉
    static readonly Color[] Sakura =
    {
        C("#F2A88F"), C("#F49BAE"), C("#EE85AC"), C("#E370AA"),
        C("#D081C4"), C("#B98FD6"), C("#A79BE0"),
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

    // 0 号是免费初始主题，玩家一进商店就已经装备着它
    static readonly Color[][] ThemePalettes =
    {
        ShibuyaNight, TealOrange, HazySummer, Macaron, Sakura, Tiffany, Rainbow,
    };

    // TODO 名字是占位，等美术定了再改
    public static readonly string[] ThemeNames =
    {
        "Shibuya Night", "Teal & Orange", "Hazy Summer", "Macaron", "Sakura", "Tiffany", "Rainbow",
    };

    public static readonly int[] ThemePrices = { 0, 100, 200, 200, 250, 300, 300 };

    public static int ThemeCount => ThemePalettes.Length;

    public static Color[] ForTheme(int index)
    {
        return ThemePalettes[Mathf.Clamp(index, 0, ThemePalettes.Length - 1)];
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
