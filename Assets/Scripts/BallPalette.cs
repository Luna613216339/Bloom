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

    // 岩浆：纯暖色域，酒红 → 朱红 → 橙 → 琥珀金。原来没有一套是单纯的暖色
    static readonly Color[] Magma =
    {
        C("#8E1B2E"), C("#C21E28"), C("#E24417"), C("#F26B0A"),
        C("#D99411"), C("#A8321F"), C("#6B1F3A"),
    };

    // 糖果霓虹：马卡龙的同位替身，七色分布一样，饱和度拉满、明度压下来
    static readonly Color[] CandyNeon =
    {
        C("#FF2E74"), C("#F2610F"), C("#D19400"), C("#4FA82A"),
        C("#00A88F"), C("#2E8BFF"), C("#9A3FE0"),
    };

    // 翡翠林：绿为主角。色相图上最大的一块空白
    static readonly Color[] Emerald =
    {
        C("#075E3F"), C("#0E8055"), C("#17A05B"), C("#4CB33C"),
        C("#8CC220"), C("#0FA588"), C("#046B62"),
    };

    // 葡萄夜：紫为主角。白底方案里对比度最高的一套（2.05）
    static readonly Color[] Grape =
    {
        C("#4A1E8C"), C("#6A22A8"), C("#8B25B8"), C("#B02BA8"),
        C("#C92D8A"), C("#7A28C4"), C("#3A2496"),
    };

    // 复古海报：七十年代印刷品，砖红 / 芥末 / 橄榄 / 鸭青。
    // 饱和度中等但明度压得低，白底上照样立得住
    static readonly Color[] RetroPoster =
    {
        C("#B8402C"), C("#DE7020"), C("#C79213"), C("#8A9A2B"),
        C("#3D7A66"), C("#A2472A"), C("#5F7A3A"),
    };

    // 包豪斯：红黄蓝三原色 + 一颗近黑。
    // 那颗黑球是全场对比度最高的一颗 —— 别的球爆炸时在褪色，它在变清楚
    static readonly Color[] Bauhaus =
    {
        C("#D62828"), C("#D98B0A"), C("#1D5FA8"), C("#2A2A2A"),
        C("#C1121F"), C("#3E7A3A"), C("#14487A"),
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
    // ⚠️ 只能往后追加，不能插队或调顺序 —— 存档里的"已购买/已装备"存的是索引，
    // 顺序一变，玩家买过的主题就会错位到别的主题上
    static readonly Color[][] ThemePalettes =
    {
        ShibuyaNight, TealOrange, HazySummer, Macaron, Sakura, Tiffany, Rainbow,
        ShibuyaNightDark,
        CandyNeon, Magma, Emerald, Grape, RetroPoster, Bauhaus,
        Aurora, MidnightGarden,
    };

    // TODO 名字是占位，等美术定了再改
    public static readonly string[] ThemeNames =
    {
        "Shibuya Night", "Teal & Orange", "Hazy Summer", "Macaron", "Sakura", "Tiffany", "Rainbow",
        "Shibuya Night · Dark",
        "Candy Neon", "Magma", "Emerald", "Grape", "Retro Poster", "Bauhaus",
        "Aurora", "Midnight Garden",
    };

    /// <summary>主题名的中文版，索引和 ThemeNames 一一对应。取名走 Loc.ThemeName(i)</summary>
    public static readonly string[] ThemeNamesZh =
    {
        "涩谷夜", "青橙对撞", "朦胧夏日", "马卡龙", "樱花藕粉", "蒂芙尼", "原版彩虹",
        "涩谷夜 · 深色版",
        "糖果霓虹", "岩浆", "翡翠林", "葡萄夜", "复古海报", "包豪斯",
        "极光", "午夜植物园",
    };

    /// <summary>按当前语言取主题名</summary>
    public static string ThemeName(int index)
    {
        int i = Clamp(index);
        return Loc.IsZh ? ThemeNamesZh[i] : ThemeNames[i];
    }

    /// <summary>
    /// 预览图的文件名，对应 Assets/Resources/ThemePreview/{id}.png。
    /// 和 ThemeNames 分开：那个是给玩家看的、随时会改，这个是资源路径，改了图就丢。
    /// </summary>
    public static readonly string[] ThemeIds =
    {
        "shibuya-night", "teal-orange", "hazy-summer", "macaron", "sakura", "tiffany", "rainbow",
        "shibuya-night-dark",
        "candy-neon", "magma", "emerald", "grape", "retro-poster", "bauhaus",
        "aurora", "midnight-garden",
    };

    /// <summary>
    /// 深色主题定在 300：换深色不是换配色，是换掉整个游戏的观感，价值不是一个档次。
    /// 但涩谷夜·深色版留 0 —— 先让玩家白拿一套尝到"原来还能这样"，
    /// 才会为第二套深色掏钱。免费的那套是品类的样品，不是施舍。
    /// </summary>
    public static readonly int[] ThemePrices =
    {
        0, 50, 100, 100, 200, 200, 300,
        0,
        150, 150, 150, 200, 200, 250,
        300, 300,
    };

    /// <summary>
    /// 每个主题的背景色。白底是这个游戏的默认，深色主题各自带自己的底。
    ///
    /// 深色底不用纯黑：纯黑配霓虹会产生刺眼的振动感，而且任何 UI 接缝都会露出来。
    /// 抬一点、往紫偏一点，才读得出"夜"而不是"关掉的屏幕"。
    /// </summary>
    static readonly Color[] ThemeBackgrounds =
    {
        Color.white, Color.white, Color.white, Color.white,
        Color.white, Color.white, Color.white,
        C("#15121E"),                                     // 涩谷夜·深色版
        Color.white, Color.white, Color.white,            // 糖果霓虹 / 岩浆 / 翡翠林
        Color.white, Color.white, Color.white,            // 葡萄夜 / 复古海报 / 包豪斯
        C("#0A1024"),                                     // 极光：近黑的夜蓝
        C("#0D1512"),                                     // 午夜植物园：墨绿黑
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
