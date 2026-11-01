using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全部正式关卡，按顺序写在这里。加一关 = 加一个方法并登记到 Build() 里。
/// 关卡的设计说明和理由在 Docs/关卡表.md，这里只放数据和一句话备注。
///
/// 坐标：相机正交尺寸 5，画面是 16:9，所以世界范围 x ∈ ±8.89，y ∈ ±5。
/// 连锁的"够得着"距离：普通散布关（球 0.4，膨胀到 1.48）是 0.94；
/// 蛇形关（球 0.35，膨胀到 1.3）是 0.825。摆阵时间距小于它才会传下去。
/// </summary>
public static class LevelCatalog
{
    static List<LevelDef> all;

    public static IReadOnlyList<LevelDef> All
    {
        get
        {
            if (all == null) Build();
            return all;
        }
    }

    public static int Count => All.Count;

    /// <summary>关号 1 起。越界返回 null</summary>
    public static LevelDef Get(int number)
    {
        var list = All;
        if (number < 1 || number > list.Count) return null;
        return list[number - 1];
    }

    static void Build()
    {
        all = new List<LevelDef>();
        Add(FirstBloom());      // 1
        Add(OneLine());         // 2
        Add(Ripple());          // 3
        Add(Stars());           // 4
        Add(Gears());           // 5
        Add(LongWave());        // 6
        Add(Gate());            // 7
        Add(Net());             // 8
        Add(Thicket());         // 9
        Add(Crossing());        // 10
        Add(Pincer());          // 11
        Add(Kaleidoscope());    // 12
        // ---- 以下是新机制的样关，用户试玩拍板前不算正式序列 ----
        Add(SampleSpark());     // 13 分裂球
        Add(SampleBackfire());  // 14 分裂球
        Add(SampleSwitch());    // 15 分裂球 + 门
        Add(SampleShell());     // 16 硬球
        Add(SampleFork());      // 17 分裂球 + 联动门
    }

    static void Add(LevelDef d)
    {
        d.Number = all.Count + 1;
        if (d.Puzzle) d.AllowContinue = false;
        all.Add(d);
    }

    // ---- 常量：旧关卡实测值 ----
    const float ScatterScale = 1.48f;   // 旧 1-5 关的膨胀倍数
    const float SchoolScale = 1.3f;     // 旧 6-10 关的膨胀倍数

    // =====================================================================
    // 第一章  开花
    // =====================================================================

    /// <summary>旧第 1 关原样：20 颗随机散布，点一下看它开</summary>
    static LevelDef FirstBloom()
    {
        var d = new LevelDef { Id = "first-bloom", NameZh = "初开", NameEn = "First Bloom", Target = 3,
            Palette = BallPalette.HazySummer };
        d.Scatter.Add(new ScatterDef { Count = 20 });
        return d;
    }

    /// <summary>旧第 6 关原样：一条 8 颗的蛇斜着走，带波浪</summary>
    static LevelDef OneLine()
    {
        var d = SchoolLevel("one-line", "一线", "One Line", 8, BallPalette.ShibuyaNight);
        var s = SchoolDef.Line(new Vector2(-6.4f, -3.16f), new Vector2(7.14f, 3.17f), 8);
        s.Spacing = 0.69f; s.Speed = 1.69f;
        d.Schools.Add(s);
        return d;
    }

    /// <summary>
    /// 新：12 条辐条 × 5 圈，静止。最里圈挨得近会互相传，外圈只沿辐条传。
    /// 点任何一颗球，火先聚到中心再一圈圈往外扩 —— 涟漪。点空地就什么都不发生。
    /// </summary>
    static LevelDef Ripple()
    {
        var d = new LevelDef { Id = "ripple", NameZh = "涟漪", NameEn = "Ripple", Target = 60,
            Palette = BallPalette.ShibuyaNight, ExpandSpeed = 1.5f };
        float[] radii = { 0.85f, 1.75f, 2.65f, 3.55f, 4.45f };
        for (int ring = 0; ring < radii.Length; ring++)
            LevelDef.Ring(d.Balls, Vector2.zero, radii[ring], 12, 90f);
        return d;
    }

    /// <summary>旧第 3 关：40 颗随机散布，目标 8，喘口气</summary>
    static LevelDef Stars()
    {
        var d = new LevelDef { Id = "stars", NameZh = "繁星", NameEn = "Stars", Target = 8,
            Palette = BallPalette.Tiffany };
        d.Scatter.Add(new ScatterDef { Count = 40 });
        return d;
    }

    /// <summary>旧第 9 关原样：两条圆弧相向转，像一对齿轮。要全中得等两条弧同时到中间</summary>
    static LevelDef Gears()
    {
        var d = SchoolLevel("gears", "齿轮", "Gears", 18, BallPalette.Sakura);
        var a = SchoolDef.Orbit(new Vector2(-3.26f, -0.27f), 2.68f, 9, false, 0f);
        var b = SchoolDef.Orbit(new Vector2(3.29f, -0.27f), 2.68f, 9, true, 0f);
        foreach (var s in new[] { a, b })
        {
            s.Spacing = 0.53f; s.Speed = 2.28f; s.ShowPath = true;
            d.Schools.Add(s);
        }
        return d;
    }

    /// <summary>新：一条横贯全屏的静止正弦波，从点的地方往两头依次点亮</summary>
    static LevelDef LongWave()
    {
        var d = new LevelDef { Id = "long-wave", NameZh = "长波", NameEn = "Long Wave", Target = 36,
            Palette = BallPalette.Tiffany };
        LevelDef.SineWave(d.Balls, -8.2f, 8.2f, 0f, 3.2f, 1.5f, 36);
        return d;
    }

    // =====================================================================
    // 第二章  门
    // =====================================================================

    /// <summary>
    /// 新，教门：一队球沿水平线走，中途一道关着的闸。闸不开，整队停在闸前；
    /// 开了闸，队伍走进右边那一小圈球里，这时点队头才够数。
    /// </summary>
    static LevelDef Gate()
    {
        var d = SchoolLevel("gate", "闸", "The Gate", 16, BallPalette.Emerald);
        var s = SchoolDef.Line(new Vector2(-9.6f, 0f), new Vector2(9.6f, 0f), 10);
        s.HoldWhenBlocked = true;
        d.Schools.Add(s);
        d.Walls.Add(new WallDef(1.0f, 3.1f, 0.3f, 3.8f));
        d.Walls.Add(new WallDef(1.0f, -3.1f, 0.3f, 3.8f));
        d.Doors.Add(new DoorDef(1.0f, 0f, 90f) { Size = new Vector2(2.4f, 0.37f) });
        LevelDef.Cluster(d.Balls, new Vector2(5.2f, 0f), 0.7f, 6);
        return d;
    }

    /// <summary>旧第 7 关原样：蛇尾三颗逃跑球，笼子 + 一扇门，收网</summary>
    static LevelDef Net()
    {
        var d = SchoolLevel("net", "收网", "The Net", 10, BallPalette.HazySummer);
        var s = SchoolDef.Line(new Vector2(-8.32f, -4.49f), new Vector2(7.14f, 3.17f), 10);
        s.Escape = 3;
        d.Schools.Add(s);
        d.Cages.Add(new CageDef(0.89f, 0.08f, 2.6f, 2.53f, 26.9f, true, false));
        d.Doors.Add(new DoorDef(-0.28f, -0.52f, -63.2f));
        return d;
    }

    /// <summary>旧第 5 关：60 颗随机散布，目标 16。门那几关之间的喘息</summary>
    static LevelDef Thicket()
    {
        var d = new LevelDef { Id = "thicket", NameZh = "密林", NameEn = "Thicket", Target = 16,
            Palette = BallPalette.TealOrange };
        d.Scatter.Add(new ScatterDef { Count = 60 });
        return d;
    }

    /// <summary>
    /// 新，时机：两队球从两角相向走，在中心交叉，各自路上有一道关着的闸。
    /// 先到的那队会先停在闸前；等两队都到了再一起放行，它们才会同时经过交叉点。
    /// </summary>
    static LevelDef Crossing()
    {
        var d = SchoolLevel("crossing", "错车", "Crossing", 14, BallPalette.Tiffany);
        var a = SchoolDef.Line(new Vector2(-9.6f, -4.6f), new Vector2(9.6f, 4.6f), 8);
        var b = SchoolDef.Line(new Vector2(9.6f, -4.6f), new Vector2(-9.6f, 4.6f), 8);
        a.Wave = false; b.Wave = false;
        a.HoldWhenBlocked = true; b.HoldWhenBlocked = true;
        a.Palette = BallPalette.Tiffany; b.Palette = BallPalette.Sakura;
        d.Schools.Add(a); d.Schools.Add(b);
        float ang = Mathf.Atan2(4.6f, 9.6f) * Mathf.Rad2Deg;   // 25.6°
        d.Doors.Add(new DoorDef(-1.5f, -0.72f, ang + 90f));
        d.Doors.Add(new DoorDef(1.5f, -0.72f, -ang - 90f));
        return d;
    }

    /// <summary>旧第 8 关原样：两条蛇相向而行，各带两颗逃跑球，中间一个双门笼子</summary>
    static LevelDef Pincer()
    {
        var d = SchoolLevel("pincer", "合围", "Pincer", 12, BallPalette.HazySummer);
        var a = SchoolDef.Line(new Vector2(-7.01f, -3.95f), new Vector2(6.89f, 3.97f), 6);
        var b = SchoolDef.Line(new Vector2(6.89f, 3.97f), new Vector2(-7.0f, -3.96f), 6);
        a.Escape = 2; b.Escape = 2;
        d.Schools.Add(a); d.Schools.Add(b);
        d.Cages.Add(new CageDef(0.019f, 0.029f, 3.8f, 1.91f, 29.9f, true, true));
        d.Doors.Add(new DoorDef(-1.614f, -0.916f, -60.2f, startsOpen: true));
        d.Doors.Add(new DoorDef(1.673f, 0.965f, -60.2f, startsOpen: true));
        return d;
    }

    /// <summary>旧第 10 关原样：五条圆弧绕着中心转，要一次全中</summary>
    static LevelDef Kaleidoscope()
    {
        var d = SchoolLevel("kaleidoscope", "万花筒", "Kaleidoscope", 40, BallPalette.Rainbow);
        var centers = new[]
        {
            (new Vector2(-0.02f, 2.3f), 44.2f),
            (new Vector2(-2.3976412f, 0.57254255f), 116.2f),
            (new Vector2(-1.4894634f, -2.222542f), 188.2f),
            (new Vector2(1.4494628f, -2.2225428f), 260.1f),
            (new Vector2(2.3576415f, 0.5725424f), -27.8f),
        };
        foreach (var (c, phase) in centers)
        {
            var s = SchoolDef.Orbit(c, 2.5f, 8, false, phase);
            s.Speed = 1.65f; s.ShowPath = true;
            d.Schools.Add(s);
        }
        return d;
    }

    // =====================================================================
    // 样关：分裂球
    // =====================================================================

    /// <summary>火星从分裂球飞出去的方向（背着火源 ± 这个角度），样关摆目标时用它算位置</summary>
    static Vector2 SparkDir(float forwardDeg, float sign)
    {
        float a = (forwardDeg + sign * Ball.SplitHalfAngleDeg) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    /// <summary>
    /// 样关 · 火种：左边一团球，一条桥通到一颗五彩球；点左边那团，火顺着桥烧到五彩球，
    /// 炸出两颗火星，飞向右上、右下的两小团。教"五彩球会把火带到远处"。
    /// </summary>
    static LevelDef SampleSpark()
    {
        var d = new LevelDef { Id = "spark", NameZh = "火种", NameEn = "Spark", Target = 24,
            Palette = BallPalette.HazySummer, Sample = true, Puzzle = true };
        LevelDef.Cluster(d.Balls, new Vector2(-5.6f, 0f), 0.7f, 6);
        LevelDef.Line(d.Balls, new Vector2(-4.2f, 0f), new Vector2(-1.8f, 0f), 4);
        var split = new Vector2(-1.0f, 0f);
        d.Balls.Add(new BallDef(split.x, split.y, BallKind.Split));
        foreach (float sign in new[] { 1f, -1f })
            LevelDef.Cluster(d.Balls, split + SparkDir(0f, sign) * 4.8f, 0.55f, 6);
        return d;
    }

    /// <summary>
    /// 样关 · 回火：第一反应是点左边那一大团，可火星会背着火源往右飞，右边什么都没有。
    /// 要想通：点五彩球的右侧，让火星往左飞到大团背后的两小团，五彩球自己的爆炸再顺桥烧回大团。
    /// </summary>
    static LevelDef SampleBackfire()
    {
        var d = new LevelDef { Id = "backfire", NameZh = "回火", NameEn = "Backfire", Target = 23,
            Palette = BallPalette.ShibuyaNight, Sample = true, Puzzle = true };
        LevelDef.Cluster(d.Balls, new Vector2(-5.2f, 0f), 0.9f, 9);
        LevelDef.Line(d.Balls, new Vector2(-3.9f, 0f), new Vector2(-1.8f, 0f), 4);
        var split = new Vector2(-1.0f, 0f);
        d.Balls.Add(new BallDef(split.x, split.y, BallKind.Split));
        foreach (float sign in new[] { 1f, -1f })
            LevelDef.Cluster(d.Balls, split + SparkDir(180f, sign) * 4.8f, 0.55f, 6);
        return d;
    }

    /// <summary>
    /// 样关 · 开关：和火种一样的布局，但下面那条火星的路上横着一扇关着的门。
    /// 门关着火星会被弹开；先开门再点火，两团都能到。教"门也能挡火星"。
    /// </summary>
    static LevelDef SampleSwitch()
    {
        var d = new LevelDef { Id = "switch", NameZh = "开关", NameEn = "Switch", Target = 24,
            Palette = BallPalette.Tiffany, Sample = true, Puzzle = true };
        LevelDef.Cluster(d.Balls, new Vector2(-6.0f, 0f), 0.7f, 6);
        LevelDef.Line(d.Balls, new Vector2(-4.6f, 0f), new Vector2(-2.5f, 0f), 4);
        var split = new Vector2(-1.7f, 0f);
        d.Balls.Add(new BallDef(split.x, split.y, BallKind.Split));
        foreach (float sign in new[] { 1f, -1f })
            LevelDef.Cluster(d.Balls, split + SparkDir(0f, sign) * 5.2f, 0.55f, 6);
        var doorPos = split + SparkDir(0f, -1f) * 2.6f;
        d.Doors.Add(new DoorDef(doorPos.x, doorPos.y, 90f - Ball.SplitHalfAngleDeg));
        return d;
    }

    // =====================================================================
    // 样关：硬球
    // =====================================================================

    /// <summary>
    /// 样关 · 硬壳：一排球，正中间一颗硬球。从一头点，火烧到硬球只能敲裂它的壳，停住。
    /// 紧挨着硬球点：你点出的圈是第一把火，旁边被点着的球是第二把，硬球才会炸，两头全亮。
    /// </summary>
    static LevelDef SampleShell()
    {
        var d = new LevelDef { Id = "shell", NameZh = "硬壳", NameEn = "Hard Shell", Target = 17,
            Palette = BallPalette.Emerald, Sample = true, Puzzle = true };
        LevelDef.Line(d.Balls, new Vector2(-6f, 0f), new Vector2(6f, 0f), 17);
        var mid = d.Balls[8]; mid.Kind = BallKind.Hard; d.Balls[8] = mid;
        return d;
    }

    // =====================================================================
    // 样关：联动门
    // =====================================================================

    /// <summary>
    /// 样关 · 分岔：两颗火星各走一条路，路上各一扇门，两扇门是联动的（一开另一关）。
    /// 上面那扇离得近、一开始开着；下面那扇远、关着。上面的火星过门之后再点一下门，
    /// 下面的火星才赶得上开门。联动门把"先把门都开好"变成了"在飞行中切换"。
    /// </summary>
    static LevelDef SampleFork()
    {
        var d = new LevelDef { Id = "fork", NameZh = "分岔", NameEn = "Fork", Target = 23,
            Palette = BallPalette.TealOrange, Sample = true, Puzzle = true };
        LevelDef.Cluster(d.Balls, new Vector2(-6.2f, 0f), 0.7f, 6);
        LevelDef.Line(d.Balls, new Vector2(-4.8f, 0f), new Vector2(-2.7f, 0f), 4);
        var split = new Vector2(-1.9f, 0f);
        d.Balls.Add(new BallDef(split.x, split.y, BallKind.Split));
        var up = SparkDir(0f, 1f);
        var down = SparkDir(0f, -1f);
        LevelDef.Cluster(d.Balls, split + up * 4.6f, 0.55f, 6);
        LevelDef.Cluster(d.Balls, split + down * 6.0f, 0.55f, 6);
        var d1 = split + up * 1.5f;
        var d2 = split + down * 4.2f;
        d.Doors.Add(new DoorDef(d1.x, d1.y, -(90f - Ball.SplitHalfAngleDeg), startsOpen: true, link: 1));
        d.Doors.Add(new DoorDef(d2.x, d2.y, 90f - Ball.SplitHalfAngleDeg, startsOpen: false, link: 1));
        return d;
    }

    // ---- 公用 ----

    /// <summary>蛇形关的底子：球 0.35、膨胀 1.3、涟漪式慢膨胀，和旧 6-10 关一致</summary>
    static LevelDef SchoolLevel(string id, string zh, string en, int target, Color[] palette)
    {
        return new LevelDef
        {
            Id = id, NameZh = zh, NameEn = en, Target = target, Palette = palette,
            BallSize = 0.35f, ReactionMaxScale = SchoolScale, ExpandSpeed = 1.5f,
        };
    }
}
