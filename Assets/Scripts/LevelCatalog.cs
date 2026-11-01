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
        const string ch1 = "正式 · 第一章 开花";
        const string ch2 = "正式 · 第二章 门";
        Add(FirstBloom(), ch1, "点一下，看它开。随机散布，目标宽松");                        // 1
        Add(OneLine(), ch1, "斜着走的一队球，火从点的地方一颗颗亮过去");                     // 2
        Add(Ripple(), ch1, "点任何一颗，火先聚到中心再一圈圈往外扩");                       // 3
        Add(Stars(), ch1, "一堆乱飞的球，找最密的地方点");                                   // 4
        Add(Gears(), ch1, "两条弧相向转，等它们同时到中间那道缝");                           // 5
        Add(LongWave(), ch1, "横贯全屏的波浪，从点的地方往两头亮");                         // 6
        Add(Gate(), ch2, "一队球被闸拦住，开闸它们才走进右边那圈（动静结合）");               // 7
        Add(Net(), ch2, "蛇尾三颗逃跑球，关门把它们关进笼子");                               // 8
        Add(Thicket(), ch2, "满屏的球，喘口气");                                             // 9
        Add(Crossing(), ch2, "两队球相向来，用门拦住先到的那队等一等");                      // 10
        Add(Pincer(), ch2, "两条蛇从两头进同一个笼子，两扇门都要关");                        // 11
        Add(Kaleidoscope(), ch2, "五条弧绕中心转，一次全中");                                // 12
        // ---- 新机制的样关：分裂球、联动门已通过，硬球用户还没看到 ----
        const string smp = "样关（新机制）";
        Add(SampleSpark(), smp, "火烧到五彩球，裂出两颗火星飞向远处的两小团");               // 13
        Add(SampleBackfire(), smp, "火星背着火源飞：点大团没用，要点五彩球的另一侧");         // 14
        Add(SampleSwitch(), smp, "火星路上有扇关着的门，先开门再点");                         // 15
        Add(SampleShell(), smp, "一排球中间一颗硬球，要两把不同的火才炸");                   // 16
        Add(SampleFork(), smp, "两颗火星各走一条路，联动门要在飞行中切换");                   // 17
        // ---- 引导关：分裂球 + 门，循序渐进，建议排在 15 和 17 之间 ----
        const string gd = "引导关（分裂球 + 门，建议排在 15 开关 和 17 分岔 之间）";
        Add(GuideBounce(), gd, "火星撞上关着的门会弹回来，弹回来的火星照样能用");             // 18
        Add(GuideReroute(), gd, "门开着火星会飞到空地；把门关上，让它弹进那一团");           // 19
        Add(GuidePrepare(), gd, "两扇普通门，一扇要开一扇要关，点火前先准备好");             // 20
        // ---- 备选池：动静结合（用户最喜欢的一类）----
        const string dyn = "备选 · 动静结合";
        Add(WaitingLine(), dyn, "队伍停在闸前才能传火；开了闸反而走散（和第 7 关相反）");     // 21
        Add(Halt(), dyn, "门开着队伍会走掉；先关门让它停在路口，再点远处那条线");             // 22
        Add(Hoop(), dyn, "一队球穿过一个静止的大环，队头在环上那一刻点");                     // 23
        Add(TwoGates(), dyn, "两队球、两道闸，把它们放到同时走进中间那个结构");               // 24
        Add(Binary(), dyn, "两条弧一内一外反向转，顶上一小团，两个队头同时到时点");           // 25
        Add(Loom(), dyn, "三列球上下穿梭，横着一条静止的线，像织布");                         // 26
        Add(Drifter(), dyn, "一团球整体漂流、撞墙反弹，等它漂过静止的那团");                 // 27
        Add(Strings(), dyn, "五根静止的弦，一队球竖着穿过去，把五根弦一起拨响");             // 28
        Add(Turnaround(), dyn, "队伍到头会折返、叠在一起，折叠那一刻最密");                   // 29
        // ---- 备选池：分裂球 ----
        const string spl = "备选 · 分裂球";
        Add(Relay(), spl, "五颗五彩球隔着够不着的距离排一排，火星一路接力过去");             // 30
        Add(Fireworks(), spl, "转动的环里嵌着五彩球，火星向四周射向小团");                   // 31
        Add(Boomerang(), spl, "火星撞上斜墙拐弯，俯冲进下面两团");                           // 32
        // ---- 备选池：硬球 ----
        const string hrd = "备选 · 硬球";
        Add(TwoLocks(), hrd, "两头是大团，中间两颗硬球夹一条双排线；点大团只敲裂，点中间全开"); // 33
        Add(Key(), hrd, "火沿着线烧到硬球只能敲裂，追上来的火星是第二把火");                 // 34
        Add(Bolt(), hrd, "队伍停在闸前，闸后一颗硬球挡着一条线；点硬球和线之间的缝");         // 35
        // ---- 备选池：门与笼子 ----
        const string dr = "备选 · 门与笼子";
        Add(ShiftChange(), dr, "两队球各有一扇联动门：切一下，走的停、停的走，让两队都进中间"); // 36
        Add(Jar(), dr, "整条蛇涌进右边封死的笼子，四颗逃跑球也进来了再关门");                 // 37
        // ---- 备选池：奇观（静止）----
        const string spc = "备选 · 奇观";
        Add(Spiral(), spc, "一条螺旋线，点中心火转着圈往外烧");                               // 38
        Add(Rose(), spc, "三片花瓣在中心相连，点中心三片一起开");                             // 39
        Add(Meander(), spc, "五行球首尾相接成一条长路，火要爬二十秒才到头");                 // 40
        // ---- 备选池：续命 ----
        Add(Encore(), "备选 · 续命", "两团离得远，一下点不完；没过关时按「再打一下」补第二下"); // 41
        // ---- 实验：静止球群的 idle，第 7 关 × 5 种动法，挨个点开比 ----
        const string idle = "实验 · 静止球群 idle（第 7 关 × 5 种动法）";
        Add(GateIdle(IdleMode.Bob), idle, "右边那圈整团上下浮（幅度 0.12）");                 // 42
        Add(GateIdle(IdleMode.Sway), idle, "右边那圈整团左右摆（幅度 0.12）");               // 43
        Add(GateIdle(IdleMode.Spin), idle, "右边那圈整团慢转（8°/秒）");                     // 44
        Add(GateIdle(IdleMode.Breathe), idle, "右边那圈呼吸：整团微微胀缩（幅度 0.05）");     // 45
        Add(GateIdle(IdleMode.Drift), idle, "右边那圈每颗各自轻微漂移（幅度 0.04）");         // 46
    }

    static void Add(LevelDef d, string group, string note)
    {
        d.Group = group;
        d.Note = note;
        Add(d);
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

    /// <summary>
    /// 旧第 9 关改造：两条圆弧相向转，像一对齿轮。要全中得在两条弧同时经过中间那道缝时点缝里。
    /// 旧场景里两个圆心相距 6.55，缝宽 1.19，自动测试扫过：只有一个 ±0.15 秒、±0.2 格的点能全中，
    /// 人打不出来。圆心改到相距 6.0（缝宽 0.64），窗口放宽到两秒左右，形状也更像咬合的齿轮。
    /// </summary>
    static LevelDef Gears()
    {
        var d = SchoolLevel("gears", "齿轮", "Gears", 18, BallPalette.Sakura);
        var a = SchoolDef.Orbit(new Vector2(-3.0f, -0.27f), 2.68f, 9, false, 0f);
        // 旧场景里右边那个圆绕 X 轴翻了 180°，Unity 把它算成 z 旋转 180°：两条弧镜像对称地转
        var b = SchoolDef.Orbit(new Vector2(3.0f, -0.27f), 2.68f, 9, true, 180f);
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
        var d = SchoolLevel("gate", "闸", "The Gate", 15, BallPalette.Emerald);
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
    /// 新，时机：两队球从两角相向走，在中心交叉。左下那队先到，右下那队晚 4 秒多才到。
    /// 什么都不做的话它们先后经过交叉点，一次点击够不着两队。两条路上各有一扇开着的门：
    /// 把先到那队的门关上让它等，等另一队快到了再开，两队就会一起过交叉点。
    /// </summary>
    static LevelDef Crossing()
    {
        var d = SchoolLevel("crossing", "错车", "Crossing", 14, BallPalette.Tiffany);
        var a = SchoolDef.Line(new Vector2(-9.6f, -4.6f), new Vector2(9.6f, 4.6f), 8);
        // 右边那队的起点拉到屏幕外更远处，所以它天然晚到
        var b = SchoolDef.Line(new Vector2(14.6f, -7.0f), new Vector2(-9.6f, 4.6f), 8);
        a.Wave = false; b.Wave = false;
        a.Speed = 2.0f; b.Speed = 2.0f;
        a.HoldWhenBlocked = true; b.HoldWhenBlocked = true;
        a.Palette = BallPalette.Tiffany; b.Palette = BallPalette.Sakura;
        d.Schools.Add(a); d.Schools.Add(b);
        float ang = Mathf.Atan2(4.6f, 9.6f) * Mathf.Rad2Deg;   // 25.6°
        d.Doors.Add(new DoorDef(-1.48f, -0.71f, ang + 90f, startsOpen: true));
        d.Doors.Add(new DoorDef(1.48f, -0.71f, -ang - 90f, startsOpen: true));
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
            Palette = BallPalette.HazySummer, Sample = true };
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
            Palette = BallPalette.Tiffany, Sample = true };
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
            Palette = BallPalette.TealOrange, Sample = true };
        LevelDef.Cluster(d.Balls, new Vector2(-6.2f, 0f), 0.7f, 6);
        LevelDef.Line(d.Balls, new Vector2(-4.8f, 0f), new Vector2(-2.7f, 0f), 4);
        var split = new Vector2(-1.9f, 0f);
        d.Balls.Add(new BallDef(split.x, split.y, BallKind.Split));
        var up = SparkDir(0f, 1f);
        var down = SparkDir(0f, -1f);
        LevelDef.Cluster(d.Balls, split + up * 4.6f, 0.55f, 6);
        LevelDef.Cluster(d.Balls, split + down * 6.2f, 0.55f, 6);
        // 上门近、下门远：两颗火星同时出发，先过上门，再过下门，中间那 0.9 秒就是切换的窗口
        var d1 = split + up * 1.0f;
        var d2 = split + down * 5.2f;
        d.Doors.Add(new DoorDef(d1.x, d1.y, -(90f - Ball.SplitHalfAngleDeg), startsOpen: true, link: 1));
        d.Doors.Add(new DoorDef(d2.x, d2.y, 90f - Ball.SplitHalfAngleDeg, startsOpen: false, link: 1));
        return d;
    }

    // =====================================================================
    // 引导关：分裂球 + 门。每关只教一件事，排在 15 开关 和 17 分岔 之间（理由见关卡表）
    // =====================================================================

    /// <summary>
    /// 引导 · 弹回：和火种一样的左团 + 桥 + 五彩球。上面那颗火星直飞右上的小团；
    /// 下面那颗火星撞上一扇斜着的关着的门，被弹成竖直向下，落进正下方那一团。
    /// 什么都不用操作，点左团就全亮 —— 只为了让玩家亲眼看见"火星会被门弹回，而且弹回来还有用"。
    /// 门近乎竖直（转 95°）：火星以 45° 入射、反射成朝左下 130° 飞。第一版让门斜 115° 想把火星
    /// 折成竖直向下，结果入射角只有 25°，火星先擦到门的端头、反射方向偏了 18°，所以改成这样。
    /// </summary>
    static LevelDef GuideBounce()
    {
        var d = new LevelDef { Id = "guide-bounce", NameZh = "弹回", NameEn = "Bounce", Target = 25,
            Palette = BallPalette.HazySummer, Pool = LevelPool.Candidate };
        var split = BuildSparkFeed(d, new Vector2(-6.2f, 0.8f), new Vector2(-1.6f, 0.8f));
        LevelDef.Cluster(d.Balls, split + SparkDir(0f, 1f) * 4.6f, 0.55f, 6);        // 右上，直达
        var doorPos = split + SparkDir(0f, -1f) * 2.4f;
        d.Doors.Add(new DoorDef(doorPos.x, doorPos.y, 95f));
        LevelDef.Cluster(d.Balls, doorPos + BounceDir * 2.4f, 0.55f, 6);              // 弹回后的落点
        return d;
    }

    /// <summary>
    /// 引导 · 改道：布局同弹回，但那扇斜门一开始是开着的 —— 火星穿过去飞到空地，什么也没有。
    /// 把门关上（点火前关，或者点火之后 0.5 秒内关都来得及），火星才会被弹进下面那团。
    /// 教"关门也是工具"：15 开关 教的是开门放火星过，这里反过来。
    /// </summary>
    static LevelDef GuideReroute()
    {
        var d = GuideBounce();
        d.Id = "guide-reroute"; d.NameZh = "改道"; d.NameEn = "Reroute"; d.Target = 24;
        d.Palette = BallPalette.Tiffany;
        d.Doors[0].StartsOpen = true;
        return d;
    }

    /// <summary>
    /// 引导 · 预备：两颗火星各走一条路，两扇普通门（不联动）：上面那扇关着（要开），
    /// 下面那扇开着（要关，关上火星才弹进下团）。两件事点火前都能准备好，没有时机压力。
    /// 这是 17 分岔 的前一步：分岔把两扇门联动起来，就只能在飞行中切了。
    /// </summary>
    static LevelDef GuidePrepare()
    {
        var d = new LevelDef { Id = "guide-prepare", NameZh = "预备", NameEn = "Prepare", Target = 24,
            Palette = BallPalette.Emerald, Pool = LevelPool.Candidate };
        var split = BuildSparkFeed(d, new Vector2(-6.2f, 0.4f), new Vector2(-1.6f, 0.4f));
        var up = SparkDir(0f, 1f);
        LevelDef.Cluster(d.Balls, split + up * 4.8f, 0.55f, 6);
        var upDoor = split + up * 2.2f;
        d.Doors.Add(new DoorDef(upDoor.x, upDoor.y, -(90f - Ball.SplitHalfAngleDeg)));          // 关着，横在路上
        var downDoor = split + SparkDir(0f, -1f) * 2.4f;
        d.Doors.Add(new DoorDef(downDoor.x, downDoor.y, 95f, startsOpen: true));                 // 开着，要关
        LevelDef.Cluster(d.Balls, downDoor + BounceDir * 2.4f, 0.55f, 6);
        return d;
    }

    /// <summary>火星撞上转 95° 的门之后的方向：入射 -40°，门的法线 5°，反射成 -130°</summary>
    static readonly Vector2 BounceDir = new Vector2(Mathf.Cos(-130f * Mathf.Deg2Rad), Mathf.Sin(-130f * Mathf.Deg2Rad));

    /// <summary>左团 + 桥 + 五彩球，火种那套喂火的布局。返回五彩球的位置</summary>
    static Vector2 BuildSparkFeed(LevelDef d, Vector2 cluster, Vector2 split)
    {
        LevelDef.Cluster(d.Balls, cluster, 0.7f, 6);
        float x0 = cluster.x + 1.4f, x1 = split.x - 0.8f;
        LevelDef.Line(d.Balls, new Vector2(x0, cluster.y), new Vector2(x1, cluster.y), 4);
        d.Balls.Add(new BallDef(split.x, split.y, BallKind.Split));
        return split;
    }

    // =====================================================================
    // 备选：动静结合
    // =====================================================================

    /// <summary>
    /// 候车：队伍被闸拦住，停成一条静止的线，穿过左边那圈；闸后紧挨着另一小团。
    /// 点左圈，火沿着停住的队伍一路烧到队头，再跳到闸后那团。
    /// 和第 7 关相反：这关开闸是错的 —— 队伍一走，火追不上它（火只能往后传，追不上往前走的队）。
    /// </summary>
    static LevelDef WaitingLine()
    {
        var d = SchoolLevel("waiting-line", "候车", "Waiting Line", 24, BallPalette.HazySummer);
        d.Pool = LevelPool.Candidate;
        var s = SchoolDef.Line(new Vector2(-9.6f, 0f), new Vector2(9.6f, 0f), 12);
        s.HoldWhenBlocked = true; s.Wave = false;
        d.Schools.Add(s);
        d.Walls.Add(new WallDef(1.0f, 3.1f, 0.3f, 3.8f));
        d.Walls.Add(new WallDef(1.0f, -3.1f, 0.3f, 3.8f));
        d.Doors.Add(new DoorDef(1.0f, 0f, 90f) { Size = new Vector2(2.4f, 0.37f) });
        LevelDef.Ring(d.Balls, new Vector2(-2.5f, 0f), 0.7f, 6, 90f);      // 队伍从圈中间穿过
        LevelDef.Ring(d.Balls, new Vector2(1.98f, 0f), 0.6f, 6, 0f);       // 闸后那团：最左那颗正对停住的队头（0.73），不碰门
        return d;
    }

    /// <summary>
    /// 拦停：左边一条长线伸到路口，一队球竖着穿过路口，右边一小团。队伍只有停在路口时才能把线和团接起来。
    /// 路口上方那扇门一开始是开着的，不关它队伍就直接走掉；关上，队头撞门停住，队身正好横在路口。
    /// 然后点线的最左端，火走 2 秒到路口 —— 队伍已经在那儿等着了。
    /// </summary>
    static LevelDef Halt()
    {
        var d = SchoolLevel("halt", "拦停", "Halt", 29, BallPalette.Tiffany);
        d.Pool = LevelPool.Candidate;
        LevelDef.Line(d.Balls, new Vector2(-7.0f, 0f), new Vector2(-0.7f, 0f), 10);
        var s = SchoolDef.Line(new Vector2(0f, -9.6f), new Vector2(0f, 9.6f), 12);
        s.HoldWhenBlocked = true; s.Wave = false;
        d.Schools.Add(s);
        d.Doors.Add(new DoorDef(0f, 2.5f, 0f, startsOpen: true));
        LevelDef.Cluster(d.Balls, new Vector2(1.3f, 0f), 0.6f, 6);     // 最左那颗离停住的队伍 0.74
        return d;
    }

    /// <summary>穿环：静止的大环，一队球沿直线从中穿过。队头刚碰到环的那一刻点队头，环和整队都亮</summary>
    static LevelDef Hoop()
    {
        var d = SchoolLevel("hoop", "穿环", "Hoop", 26, BallPalette.Sakura);
        d.Pool = LevelPool.Candidate;
        LevelDef.Ring(d.Balls, Vector2.zero, 2.0f, 16, 90f);
        var s = SchoolDef.Line(new Vector2(-9.6f, 0f), new Vector2(9.6f, 0f), 10);
        s.Wave = false; s.Speed = 1.7f;
        d.Schools.Add(s);
        return d;
    }

    /// <summary>
    /// 双闸：上下两队球各被一道闸拦着，右边一个"工"字形的静止结构横跨两条路。
    /// 下面那队起点更远、晚到。两道闸都开早了，两队一前一后过结构，一下点不全；
    /// 等两队都停在闸前再一起开，它们就并肩走进结构。
    /// </summary>
    static LevelDef TwoGates()
    {
        var d = SchoolLevel("two-gates", "双闸", "Two Gates", 32, BallPalette.Emerald);
        d.Pool = LevelPool.Candidate;
        var a = SchoolDef.Line(new Vector2(-9.6f, 1.5f), new Vector2(9.6f, 1.5f), 10);
        var b = SchoolDef.Line(new Vector2(-15.6f, -1.5f), new Vector2(9.6f, -1.5f), 10);
        foreach (var s in new[] { a, b }) { s.HoldWhenBlocked = true; s.Wave = false; d.Schools.Add(s); }
        b.LightTone = true;
        d.Doors.Add(new DoorDef(1.0f, 1.5f, 90f));
        d.Doors.Add(new DoorDef(1.0f, -1.5f, 90f));
        LevelDef.Ring(d.Balls, new Vector2(5.0f, 1.5f), 0.7f, 6, 90f);
        LevelDef.Ring(d.Balls, new Vector2(5.0f, -1.5f), 0.7f, 6, 90f);
        d.Balls.Add(new BallDef(5.0f, 0.3f));
        d.Balls.Add(new BallDef(5.0f, -0.3f));
        return d;
    }

    /// <summary>
    /// 双星：两条弧一内一外反向转，周期相同（外圈走得快）。顶上一小团正好卡在两条轨道之间。
    /// 两个队头同时到顶的那一刻点那一团，两条弧都亮。开场 4 秒第一次对上，之后每 7.6 秒一次。
    /// </summary>
    static LevelDef Binary()
    {
        var d = SchoolLevel("binary", "双星", "Binary", 20, BallPalette.ShibuyaNight);
        d.Pool = LevelPool.Candidate;
        const float rIn = 1.9f, rOut = 3.4f, speed = 1.65f;
        // 队头在路径上的起始路程是 (Count-1)*Spacing，相位要把这段弧也算进去：4 秒后两个队头都到顶（90°）
        float headArc = 7 * 0.5f;
        float speedOut = speed * rOut / rIn;
        float phaseIn = 90f - (headArc + speed * 4f) / rIn * Mathf.Rad2Deg;
        float phaseOut = 90f + (headArc + speedOut * 4f) / rOut * Mathf.Rad2Deg;
        var inner = SchoolDef.Orbit(Vector2.zero, rIn, 8, false, phaseIn);
        var outer = SchoolDef.Orbit(Vector2.zero, rOut, 8, true, phaseOut);
        inner.Speed = speed; outer.Speed = speedOut;
        inner.ShowPath = true; outer.ShowPath = true;
        outer.LightTone = true;
        d.Schools.Add(inner); d.Schools.Add(outer);
        LevelDef.Cluster(d.Balls, new Vector2(0f, (rIn + rOut) / 2f), 0.4f, 5);
        return d;
    }

    /// <summary>
    /// 经纬：三列球各自竖着穿梭（撞到上下边反弹），横着一条静止的线。
    /// 列穿过线的时候点线，火跳上正在穿过的列。像织布。
    /// </summary>
    static LevelDef Loom()
    {
        var d = new LevelDef { Id = "loom", NameZh = "经纬", NameEn = "Loom", Target = 24,
            Palette = BallPalette.TealOrange, ExpandSpeed = 1.5f, Pool = LevelPool.Candidate };
        LevelDef.Line(d.Balls, new Vector2(-6.3f, 0f), new Vector2(6.3f, 0f), 15);
        float[] xs = { -4.5f, 0f, 4.5f };
        float[] ys = { -2.5f, 2.5f, -2.5f };
        float[] vs = { 2.4f, -2.4f, 2.4f };
        for (int c = 0; c < 3; c++)
            for (int i = 0; i < 6; i++)
            {
                var b = new BallDef(xs[c], ys[c] + (i - 2.5f) * 0.7f);
                b.Vel = new Vector2(0f, vs[c]);
                d.Balls.Add(b);
            }
        return d;
    }

    /// <summary>
    /// 漂流：一团七颗球整体以同一速度漂流，撞墙一起反弹（弹性碰撞保形，队形永远不散）。
    /// 两个角落各一团静止的。漂流的那团经过静止那团时点，两团一起亮。
    /// </summary>
    static LevelDef Drifter()
    {
        var d = new LevelDef { Id = "drifter", NameZh = "漂流", NameEn = "Drifter", Target = 14,
            Palette = BallPalette.Tiffany, Pool = LevelPool.Candidate };
        var mover = new List<BallDef>();
        LevelDef.Cluster(mover, new Vector2(-2.0f, 1.0f), 0.7f, 6);
        foreach (var m in mover) { var b = m; b.Vel = new Vector2(1.4f, 1.0f); d.Balls.Add(b); }
        LevelDef.Cluster(d.Balls, new Vector2(-5.5f, -2.5f), 0.7f, 6);
        LevelDef.Cluster(d.Balls, new Vector2(5.5f, 2.5f), 0.7f, 6);
        return d;
    }

    /// <summary>
    /// 琴弦：五根静止的横线，彼此隔着够不着的距离；一队球竖着穿过去。
    /// 队伍同时跨着五根弦的那一刻点中间，火借着队伍一根根往上爬，五根弦一起响。
    /// </summary>
    static LevelDef Strings()
    {
        var d = new LevelDef { Id = "strings", NameZh = "琴弦", NameEn = "Strings", Target = 70,
            Palette = BallPalette.ShibuyaNight, ExpandSpeed = 1.5f, Pool = LevelPool.Candidate };
        for (int r = -2; r <= 2; r++)
            LevelDef.Line(d.Balls, new Vector2(-5.4f, r * 1.2f), new Vector2(5.4f, r * 1.2f), 13);
        var s = SchoolDef.Line(new Vector2(0f, -9.6f), new Vector2(0f, 9.6f), 14);
        s.Wave = false; s.Speed = 1.9f;
        d.Schools.Add(s);
        return d;
    }

    /// <summary>
    /// 折返：一队球在两团之间来回走。路到头，队头掉头往回走，后面的球还在往前 —— 整队叠在一起，
    /// 这一刻最密。队头折返时点端点那团，叠起来的整队都亮。两头都行。
    /// </summary>
    static LevelDef Turnaround()
    {
        var d = SchoolLevel("turnaround", "折返", "Turnaround", 18, BallPalette.Sakura);
        d.Pool = LevelPool.Candidate;
        var s = SchoolDef.Line(new Vector2(-4f, 0f), new Vector2(4f, 0f), 12);
        d.Schools.Add(s);
        LevelDef.Ring(d.Balls, new Vector2(4.9f, 0f), 0.6f, 6, 90f);
        LevelDef.Ring(d.Balls, new Vector2(-4.9f, 0f), 0.6f, 6, 90f);
        return d;
    }

    // =====================================================================
    // 备选：分裂球
    // =====================================================================

    /// <summary>
    /// 接力：五颗五彩球隔 3 个单位排一排，火星固定沿这条线正反飞。点任一颗，火星向两边接力，
    /// 一路跳到两头的小团。纯奇观，怎么点都过（只要点在五彩球上）。
    /// </summary>
    static LevelDef Relay()
    {
        var d = new LevelDef { Id = "relay", NameZh = "接力", NameEn = "Relay", Target = 17,
            Palette = BallPalette.HazySummer, Pool = LevelPool.Candidate };
        for (int i = -2; i <= 2; i++)
            d.Balls.Add(new BallDef(i * 3f, 0f, BallKind.Split) { SplitAxisDeg = 0f });
        LevelDef.Cluster(d.Balls, new Vector2(-8.0f, 0f), 0.5f, 5);
        LevelDef.Cluster(d.Balls, new Vector2(8.0f, 0f), 0.5f, 5);
        return d;
    }

    /// <summary>烟花：转动的环里每三颗有一颗五彩球，周围六小团。点环，火星向四周射出去</summary>
    static LevelDef Fireworks()
    {
        var d = SchoolLevel("fireworks", "烟花", "Fireworks", 20, BallPalette.Rainbow);
        d.Pool = LevelPool.Candidate;
        var s = SchoolDef.Orbit(Vector2.zero, 2.4f, 12, false, 90f);
        s.Spacing = 0.6f; s.Speed = 1.65f; s.ShowPath = true;
        s.Kinds = new BallKind[12];
        for (int i = 2; i < 12; i += 3) s.Kinds[i] = BallKind.Split;
        d.Schools.Add(s);
        for (int k = 0; k < 6; k++)
        {
            float a = k * 60f * Mathf.Deg2Rad;
            LevelDef.Cluster(d.Balls, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4.3f, 0.45f, 5);
        }
        return d;
    }

    /// <summary>
    /// 回旋：顶上一颗五彩球（外面一圈普通球），火星固定左右飞；两侧各一面 45° 的斜墙，
    /// 把火星折成竖直向下，俯冲进下面两团。
    /// </summary>
    static LevelDef Boomerang()
    {
        var d = new LevelDef { Id = "boomerang", NameZh = "回旋", NameEn = "Boomerang", Target = 21,
            Palette = BallPalette.TealOrange, Pool = LevelPool.Candidate };
        d.Balls.Add(new BallDef(0f, 3.2f, BallKind.Split) { SplitAxisDeg = 0f });
        LevelDef.Ring(d.Balls, new Vector2(0f, 3.2f), 0.7f, 6, 0f);
        // 墙的长边是局部 Y 轴：右墙转 +45° 成"\"，把向右的火星折向下；左墙镜像
        d.Walls.Add(new WallDef(3.2f, 3.2f, 0.3f, 1.8f, 45f));
        d.Walls.Add(new WallDef(-3.2f, 3.2f, 0.3f, 1.8f, -45f));
        LevelDef.Cluster(d.Balls, new Vector2(3.2f, -0.6f), 0.55f, 6);
        LevelDef.Cluster(d.Balls, new Vector2(-3.2f, -0.6f), 0.55f, 6);
        return d;
    }

    // =====================================================================
    // 备选：硬球
    // =====================================================================

    /// <summary>
    /// 双锁：两头各一大团（最想点的地方），中间两颗硬球夹着一条双排线。
    /// 点大团：火单线烧到硬球，只敲裂，停。点中间的双排线：两排各算一把火，两颗硬球一起开，两头全亮。
    /// </summary>
    static LevelDef TwoLocks()
    {
        var d = new LevelDef { Id = "two-locks", NameZh = "双锁", NameEn = "Two Locks", Target = 40,
            Palette = BallPalette.Emerald, Pool = LevelPool.Candidate, Puzzle = true };
        LevelDef.Cluster(d.Balls, new Vector2(-6.5f, 0f), 0.9f, 8);
        LevelDef.Cluster(d.Balls, new Vector2(6.5f, 0f), 0.9f, 8);
        d.Balls.Add(new BallDef(-4.7f, 0f, BallKind.Hard));
        d.Balls.Add(new BallDef(4.7f, 0f, BallKind.Hard));
        LevelDef.Line(d.Balls, new Vector2(-3.9f, 0.35f), new Vector2(3.9f, 0.35f), 10);
        LevelDef.Line(d.Balls, new Vector2(-3.9f, -0.35f), new Vector2(3.9f, -0.35f), 10);
        return d;
    }

    /// <summary>
    /// 钥匙：左团的中心是一颗五彩球（火星固定朝右飞），一条单线通到一颗硬球，硬球后面是右团。
    /// 点左团：线烧到硬球只能敲裂；1 秒后火星追到，它是第二把火，硬球炸开，右团亮。
    /// 教"火星也算一把火"。
    /// </summary>
    static LevelDef Key()
    {
        var d = new LevelDef { Id = "key", NameZh = "钥匙", NameEn = "Key", Target = 24,
            Palette = BallPalette.ShibuyaNight, Pool = LevelPool.Candidate };
        d.Balls.Add(new BallDef(-5.5f, 0f, BallKind.Split) { SplitAxisDeg = 0f });
        LevelDef.Ring(d.Balls, new Vector2(-5.5f, 0f), 0.9f, 8, 90f);
        LevelDef.Line(d.Balls, new Vector2(-4.0f, 0f), new Vector2(-0.9f, 0f), 5);
        d.Balls.Add(new BallDef(0f, 0f, BallKind.Hard));
        LevelDef.Cluster(d.Balls, new Vector2(1.7f, 0f), 0.9f, 8);
        return d;
    }

    /// <summary>
    /// 门闩：队伍停在闸前，闸后紧挨一颗硬球，硬球后面一条单线。点线：单线烧到硬球只敲裂。
    /// 点硬球和线第一颗之间的缝：点出的圈是一把火，被点着的那颗线球是第二把，硬球炸开，
    /// 火跳到闸前停着的队头，整队亮。开闸是错的。
    /// </summary>
    static LevelDef Bolt()
    {
        var d = SchoolLevel("bolt", "门闩", "Bolt", 16, BallPalette.HazySummer);
        d.Pool = LevelPool.Candidate; d.Puzzle = true;
        var s = SchoolDef.Line(new Vector2(-9.6f, 0f), new Vector2(9.6f, 0f), 10);
        s.HoldWhenBlocked = true; s.Wave = false;
        d.Schools.Add(s);
        d.Walls.Add(new WallDef(-1.0f, 2.6f, 0.3f, 2.8f));
        d.Walls.Add(new WallDef(-1.0f, -2.6f, 0.3f, 2.8f));
        d.Doors.Add(new DoorDef(-1.0f, 0f, 90f) { Size = new Vector2(2.4f, 0.37f) });
        d.Balls.Add(new BallDef(-0.6f, 0f, BallKind.Hard));
        LevelDef.Line(d.Balls, new Vector2(0.2f, 0f), new Vector2(3.4f, 0f), 5);
        return d;
    }

    // =====================================================================
    // 备选：门与笼子
    // =====================================================================

    /// <summary>
    /// 换班：上队向右走、门开着；下队向左走、门关着在等。两扇门联动。
    /// 切一下：上队在门前停住（队身正好横过中间那圈），下队开走；下队队头进圈时点圈，两队都亮。
    /// 上队队头过了门再切就晚了 —— 门在它身后，拦不住它。
    /// </summary>
    static LevelDef ShiftChange()
    {
        var d = SchoolLevel("shift-change", "换班", "Shift Change", 26, BallPalette.TealOrange);
        d.Pool = LevelPool.Candidate;
        var a = SchoolDef.Line(new Vector2(-9.6f, 1.2f), new Vector2(9.6f, 1.2f), 10);
        var b = SchoolDef.Line(new Vector2(9.6f, -1.2f), new Vector2(-9.6f, -1.2f), 10);
        foreach (var s in new[] { a, b }) { s.HoldWhenBlocked = true; s.Wave = false; d.Schools.Add(s); }
        b.LightTone = true;
        // 两扇门都在圈的右边：上队（向右）停下时队身横过圈；下队（向左）在圈右边等，一放就进圈
        d.Doors.Add(new DoorDef(2.0f, 1.2f, 90f, startsOpen: true, link: 1));
        d.Doors.Add(new DoorDef(2.0f, -1.2f, 90f, startsOpen: false, link: 1));
        LevelDef.Ring(d.Balls, Vector2.zero, 0.9f, 8, 90f);
        return d;
    }

    /// <summary>
    /// 瓮中：路笔直穿进一个右边封死的笼子。队头撞上笼底，整条蛇一颗颗涌进来、在里面乱撞；
    /// 尾巴四颗逃跑球也进来之后关门、点笼子里 —— 逃跑球往回逃撞上关着的门，留在笼子里。
    /// 第一版让路在笼子里折返，结果掉头之后"往回逃"就是朝着火跑，门根本用不上，改成这样。
    /// 和第 8 关收网同一个原理，但是正着放、逃跑球更多、笼子更窄。
    /// </summary>
    static LevelDef Jar()
    {
        // 目标 9：四颗逃跑球里总有一颗停在火够不着的角落，抓住三颗就算
        var d = SchoolLevel("jar", "瓮中", "In the Jar", 9, BallPalette.HazySummer);
        d.Pool = LevelPool.Candidate;
        var s = SchoolDef.Line(new Vector2(-9.6f, 0f), new Vector2(9.6f, 0f), 10);
        s.Escape = 4;
        d.Schools.Add(s);
        // 笼子要窄：逃跑球撞上关着的门停在门口，必须还在笼子中间那团火够得着的范围里
        d.Cages.Add(new CageDef(3.2f, 0f, 2.2f, 2.4f, 0f, true, false));
        d.Doors.Add(new DoorDef(2.1f, 0f, 90f, startsOpen: true));
        return d;
    }

    // =====================================================================
    // 备选：奇观
    // =====================================================================

    /// <summary>螺旋：阿基米德螺线，圈距 1.25（够不着），火只能顺着线转圈走</summary>
    static LevelDef Spiral()
    {
        var d = new LevelDef { Id = "spiral", NameZh = "螺旋", NameEn = "Spiral", Target = 0,
            Palette = BallPalette.Tiffany, ExpandSpeed = 3f, Pool = LevelPool.Candidate };
        // 间距 0.65、膨胀 3/秒：每跳 0.17 秒，整条线 15 秒左右烧完。0.8 + 1.5 的话要 36 秒
        Curve(d.Balls, t => { float r = 0.5f + 1.25f * t / (2f * Mathf.PI); return new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r; },
            0f, 3.3f * 2f * Mathf.PI, 0.65f);
        d.Target = d.Balls.Count;
        return d;
    }

    /// <summary>三叶：玫瑰线 r = 4.2 cos 3θ，三片花瓣在中心相连</summary>
    static LevelDef Rose()
    {
        var d = new LevelDef { Id = "rose", NameZh = "三叶", NameEn = "Rose", Target = 0,
            Palette = BallPalette.Sakura, ExpandSpeed = 1.5f, Pool = LevelPool.Candidate };
        Curve(d.Balls, t => { float r = 4.2f * Mathf.Cos(3f * t); return new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r; },
            0f, Mathf.PI, 0.8f);
        d.Target = d.Balls.Count;
        return d;
    }

    /// <summary>蜿蜒：五行球，行与行够不着，只在两端交替相连，拼成一条 99 颗的长路</summary>
    static LevelDef Meander()
    {
        var d = new LevelDef { Id = "meander", NameZh = "蜿蜒", NameEn = "Meander", Target = 0,
            Palette = BallPalette.Emerald, ExpandSpeed = 3f, Pool = LevelPool.Candidate };
        // 行内间距 0.7、膨胀 3/秒：每跳 0.2 秒，一百颗 20 秒烧完
        for (int r = 0; r < 5; r++)
        {
            float y = 3.2f - r * 1.6f;
            LevelDef.Line(d.Balls, new Vector2(-6.3f, y), new Vector2(6.3f, y), 19);
            if (r < 4)
            {
                float x = r % 2 == 0 ? 6.3f : -6.3f;
                d.Balls.Add(new BallDef(x, y - 0.8f));
            }
        }
        d.Target = d.Balls.Count;
        return d;
    }

    /// <summary>按弧长等距地沿一条参数曲线摆球</summary>
    static void Curve(List<BallDef> into, System.Func<float, Vector2> f, float t0, float t1, float spacing)
    {
        const int fine = 4000;
        Vector2 prev = f(t0);
        into.Add(new BallDef(prev.x, prev.y));
        float acc = 0f;
        for (int i = 1; i <= fine; i++)
        {
            var p = f(Mathf.Lerp(t0, t1, i / (float)fine));
            acc += Vector2.Distance(prev, p);
            prev = p;
            if (acc >= spacing) { into.Add(new BallDef(p.x, p.y)); acc = 0f; }
        }
    }

    // =====================================================================
    // 备选：续命
    // =====================================================================

    /// <summary>
    /// 再来一下：两团离得远，一下只能点着一团，目标却要两团。失败面板上按「再打一下」补第二下。
    /// 这关只为了把续命的流程摆到台面上看，不建议进正式序列（它等于逼玩家花钱）。
    /// </summary>
    static LevelDef Encore()
    {
        var d = new LevelDef { Id = "encore", NameZh = "再来一下", NameEn = "Encore", Target = 18,
            Palette = BallPalette.HazySummer, Pool = LevelPool.Candidate };
        // 半径 0.85：散布型关卡够得着的距离是 0.94，第一版写 1.0 结果外圈点不着
        LevelDef.Cluster(d.Balls, new Vector2(-4.5f, 0f), 0.85f, 8);
        LevelDef.Cluster(d.Balls, new Vector2(4.5f, 0f), 0.85f, 8);
        return d;
    }

    // =====================================================================
    // 实验：第 7 关 × idle 动法
    // =====================================================================

    static LevelDef GateIdle(IdleMode mode)
    {
        var d = Gate();
        d.Id = "gate-idle-" + mode.ToString().ToLower();
        d.NameZh = "闸 · " + IdleNameZh(mode);
        d.NameEn = "Gate · " + mode;
        d.Pool = LevelPool.Experiment;
        d.Idle = IdleDef.Of(mode);
        return d;
    }

    static string IdleNameZh(IdleMode mode)
    {
        switch (mode)
        {
            case IdleMode.Bob: return "上下浮";
            case IdleMode.Sway: return "左右摆";
            case IdleMode.Spin: return "慢转";
            case IdleMode.Breathe: return "呼吸";
            case IdleMode.Drift: return "各自漂";
            default: return "静止";
        }
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
