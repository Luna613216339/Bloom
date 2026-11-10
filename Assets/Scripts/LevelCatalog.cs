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

    /// <summary>
    /// 关卡顺序用哪一套：true = 正式序列草案（Docs/正式序列草案.md，六章），false = 第三阶段的测试顺序（按机制分段）。
    /// 2026-11-10 Fable 排的草案，要用户试玩确认；想退回旧顺序把这里改成 false 就行，关卡本身不变。
    /// 自动测试按关卡 id 找关，不受顺序影响。
    /// </summary>
    public static readonly bool DraftSequence = true;

    static void Build()
    {
        all = new List<LevelDef>();
        // 过关一律要清空全场（Add 里统一设 Target = TotalBalls）。
        // 随机散布的喘息关（旧 初开 / 繁星 / 密林）是无尽模式的玩法，2026-11-02 用户定：只留在无尽模式里。
        if (DraftSequence) BuildDraft(); else BuildLegacy();
    }

    /// <summary>正式序列草案：六章，每章六关。每关为什么放在这里见 Docs/正式序列草案.md</summary>
    static void BuildDraft()
    {
        const string c1 = "第一章 开花";
        Add(LongWave(), c1, "点任何一颗球，火从那里往两头亮过去（第一下）");                       // 1
        Add(OneLine(), c1, "会走的队伍：火只往后传，前面的球会走掉 —— 点队头");                     // 2
        Add(Ripple(), c1, "点任何一颗，火先聚到中心再一圈圈往外扩（喘息）");                         // 3
        Add(Gears(), c1, "两条弧相向转，等两个队头同时到中间那道缝");                               // 4
        Add(Hoop(), c1, "一队球穿过一个静止的大环：让队伍把火带进环里（有好几种点法）");             // 5
        Add(Kaleidoscope(), c1, "五条弧绕中心转，一次全中（简单关，章末奇观）");                     // 6

        const string c2 = "第二章 门";
        Add(Breathe(Gate()), c2, "一队球被闸拦住，开闸它们才走进右边那圈");                         // 7
        Add(Breathe(WaitingLine()), c2, "和上一关相反：别开闸，队伍停成一条线火才传得过去");         // 8
        Add(Breathe(Halt()), c2, "门开着队伍会走掉；先关门让它停在路口，把线和团接起来");           // 9
        Add(Crossing(), c2, "两队球先后到路口，用门拦住先到的那队等一等");                           // 10
        Add(Breathe(TwoGates()), c2, "两队球、两道闸，等两队都到了再一起放");                       // 11
        Add(Rose(), c2, "三片花瓣在中心相连，点中心三片一起开（喘息）");                             // 12

        const string c3 = "第三章 笼与穿梭";
        Add(Net(), c3, "蛇尾三颗蓝色的逃跑球会躲火：关门把它们关进笼子再点");                       // 13
        Add(Pincer(), c3, "两条蛇从两头进同一个笼子，两扇门都要关");                                 // 14
        Add(Jar(), c3, "整条蛇盘进右边的笼子，四颗逃跑球也进来了再关门");                           // 15
        Add(Loom(), c3, "三列球上下穿梭，横着一条静止的线：有球正穿过线时点线");                     // 16
        Add(Drift(), c3, "一团球在框里漂，撞墙反弹：等它回来穿过中间那条线时点线");                 // 17
        Add(Strings(), c3, "五根弦彼此够不着，一队球竖着穿过去，把五根弦一起拨响（章末奇观）");     // 18

        const string c4 = "第四章 火星";
        Add(Breathe(SampleSpark()), c4, "五彩球把火沿着虚线带到远处");                               // 19
        Add(Breathe(Relay()), c4, "五颗五彩球排一排，火星沿着虚线一路接力过去（喘息）");             // 20
        Add(Breathe(GuideBounce()), c4, "虚线碰到关着的门会折：火星被弹进左边那团（看就行）");       // 21
        Add(Breathe(GuideReroute()), c4, "门开着，虚线穿过去飞到空地；关上门，线折进下面那团");       // 22
        Add(Breathe(GuidePrepare()), c4, "两扇门：一扇挡着路要开，一扇要关上借它折一下");             // 23
        Add(Breathe(SampleFork()), c4, "两扇门联动、永远一开一关：只能趁火星在飞的时候切");           // 24

        const string c5 = "第五章 时机";
        Add(Breathe(SampleRelease()), c5, "虚线落在路上那一点：开闸放队伍走，让火星正好砸在队头上"); // 25
        Add(Breathe(Meteor()), c5, "五彩球自己在飞，两条虚线跟着扫：扫到两团、又在线顶上时点");     // 26
        Add(Breathe(Binary()), c5, "两条弧一内一外反向转，两个队头同时到顶上那团时点");             // 27
        Add(ShiftChange(), c5, "两队球各有一扇联动门：切一下，走的停、停的走");                     // 28
        Add(Breathe(SampleBackfire()), c5, "这颗五彩球没有虚线：火星背着火来的方向飞，要从上面点着它"); // 29
        Add(Breathe(Boomerang()), c5, "没有虚线：从哪边点着决定火星往哪飞，只有一个方向能折进两团"); // 30

        const string c6 = "第六章 硬壳";
        Add(SampleShell(), c6, "硬球要打两次：第一下碎壳，隔一会儿再来一把火才炸");                 // 31
        Add(Breathe(Key()), c6, "火星也算一把火：它抄近路先敲碎壳，绕远的火后到");                   // 32
        Add(Spiral(), c6, "一条螺旋线，点中心火转着圈往外烧（喘息）");                               // 33
        Add(Breathe(TwoLocks()), c6, "点五彩球只开得了一把锁：想想另一把为什么不开，该从哪头点");   // 34
        Add(Breathe(Bolt()), c6, "先点硬球和线之间的缝，等一拍再开闸：撞上来的队头是第二把火");     // 35
        Add(Meander(), c6, "五行球首尾相接成一条长路（终曲，只管好看）");                           // 36
    }

    /// <summary>第三阶段的测试顺序（2026-11-03，按机制分段），留着方便回退对照</summary>
    static void BuildLegacy()
    {
        const string ch1 = "正式 · 第一章 开花";
        const string ch2 = "正式 · 第二章 门";
        Add(OneLine(), ch1, "斜着走的一队球，火从点的地方一颗颗亮过去");                     // 1
        Add(Ripple(), ch1, "点任何一颗，火先聚到中心再一圈圈往外扩");                       // 2
        Add(Gears(), ch1, "两条弧相向转，等它们同时到中间那道缝");                           // 3
        Add(LongWave(), ch1, "横贯全屏的波浪，从点的地方往两头亮");                         // 4
        Add(Breathe(Gate()), ch2, "一队球被闸拦住，开闸它们才走进右边那圈（动静结合）");               // 5
        Add(Net(), ch2, "蛇尾三颗逃跑球，关门把它们关进笼子");                               // 6
        Add(Crossing(), ch2, "两队球相向来，用门拦住先到的那队等一等");                      // 7
        Add(Pincer(), ch2, "两条蛇从两头进同一个笼子，两扇门都要关");                        // 8
        Add(Kaleidoscope(), ch2, "五条弧绕中心转，一次全中（简单关）");                      // 9
        const string smp = "样关（新机制）";
        Add(Breathe(SampleSpark()), smp, "引线通到五彩球，火星像喷泉分成两股飞向上方两条线");               // 10
        Add(Breathe(SampleBackfire()), smp, "火星背着火源飞：点底下大团没用，要点五彩球的上面");         // 11
        Add(Breathe(SampleRelease()), smp, "开闸放队伍走，掐准时间点引线，火星正好砸在队头上");                         // 12
        Add(SampleShell(), smp, "一圈球顶上一颗硬球：火打掉壳之后，要绕一圈回来再打一次");                   // 13
        Add(Breathe(SampleFork()), smp, "火星一上一下，两扇联动门一开一关，要在飞行中切换");                   // 14
        const string gd = "引导关（分裂球 + 门）";
        Add(Breathe(GuideBounce()), gd, "下落的火星撞上斜门弹回来，落进左边那团（看就行，不用操作）");             // 15
        Add(Breathe(GuideReroute()), gd, "门开着火星飞到空地；把门关上，让它弹进下面那团");           // 16
        Add(Breathe(GuidePrepare()), gd, "两扇普通门，一扇要开一扇要关，点火前先准备好");             // 17
        const string dyn = "备选 · 动静结合";
        Add(Breathe(WaitingLine()), dyn, "队伍停在闸前才能传火；开了闸反而走散（和 闸 相反）");       // 18
        Add(Breathe(Halt()), dyn, "门开着队伍会走掉；先关门让它停在路口，再点远处那条线");             // 19
        Add(Hoop(), dyn, "一队球穿过一个静止的大环，队头在环上那一刻点");                     // 20
        Add(Breathe(TwoGates()), dyn, "两队球、两道闸，把它们放到同时走进中间那个结构");               // 21
        Add(Breathe(Binary()), dyn, "两条弧一内一外反向转，顶上一小团，两个队头同时到时点");           // 22
        Add(Loom(), dyn, "三列球上下穿梭，横着一条静止的线，像织布");                         // 23
        Add(Strings(), dyn, "五根静止的弦，一队球竖着穿过去，把五根弦一起拨响");             // 24
        Add(Drift(), dyn, "一团球在框里漂来漂去，趁它穿过中间那条线的时候点线");            // 25
        const string spl = "备选 · 分裂球";
        Add(Breathe(Relay()), spl, "五颗五彩球隔着够不着的距离排一排，火星一路接力过去"); // 26
        Add(Breathe(Meteor()), spl, "一颗五彩球横着来回飞，趁它经过线顶那一刻点线"); // 27
        Add(Breathe(Boomerang()), spl, "从哪边点决定火星往哪飞：只有从正下方点才撞上斜墙折进两团"); // 28
        const string hrd = "备选 · 硬球";
        Add(Breathe(TwoLocks()), hrd, "点五彩球只开得了一把锁：想想另一把为什么不开，该从哪头点"); // 29
        Add(Breathe(Key()), hrd, "火星也算一把火：它抄近路先敲碎壳，绕远的火后到"); // 30
        Add(Breathe(Bolt()), hrd, "先点硬球和线之间的缝，等一拍再开闸：撞上来的队头是第二把火"); // 31
        const string dr = "备选 · 门与笼子";
        Add(ShiftChange(), dr, "两队球各有一扇联动门（画出了路线）：切一下，走的停、停的走"); // 32
        Add(Jar(), dr, "整条蛇涌进右边封死的笼子，四颗逃跑球也进来了再关门"); // 33
        const string spc = "调剂 · 奇观";
        Add(Spiral(), spc, "一条螺旋线，点中心火转着圈往外烧"); // 34
        Add(Rose(), spc, "三片花瓣在中心相连，点中心三片一起开"); // 35
        Add(Meander(), spc, "五行球首尾相接成一条长路，火要爬二十秒才到头"); // 36
        // 撤下的（2026-11-02 用户试玩后）：旧漂流（两个角落各一团，一下够不着全部；动法在新的 漂流 里）、
        // 折返（队伍碰到球群就掉头，玩家会困惑）、再来一下（等于逼玩家花钱续命）、idle 五个对比变体（已按关挂到正式关上）。
    }

    /// <summary>这关的圆形小团用"呼吸"当 idle（长线、大图案、蛇不受影响，见 IdleGroup）</summary>
    static LevelDef Breathe(LevelDef d)
    {
        d.Idle = IdleDef.Of(IdleMode.Breathe);
        return d;
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
        d.Target = d.TotalBalls;
        // 每一队会走的球都把路画出来（细实线）：队伍往哪走、两队在哪交汇，点之前就看得见。
        // 细实线 = 队伍的路，点线 = 火星的路（AimLine）。2026-11-10 Fable 定，要用户试玩确认
        foreach (var sc in d.Schools) sc.ShowPath = true;
        if (d.Puzzle) d.AllowContinue = false;
        all.Add(d);
    }

    // ---- 常量：旧关卡实测值 ----
    const float SchoolScale = 1.3f;     // 旧 6-10 关的膨胀倍数

    // =====================================================================
    // 第一章  开花
    // =====================================================================

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
        // 整幅图一起轻轻胀缩，像水面。外三圈只沿辐条传（辐条间距 0.9，够得着 0.94），
        // 第二圈相邻 0.906 也只剩 0.034 的余量，所以幅度压在 2.5%（辐条间距最多变 0.023）
        d.Idle = IdleDef.Of(IdleMode.Breathe);
        d.Idle.Amplitude = 0.025f; d.Idle.Period = 4f; d.Idle.MaxGroupRadius = 99f;
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

    /// <summary>
    /// 收网：逃跑球第一次出现。一队球从下面游上来，钻进顶上的笼子，在里面拐个弯、队头顶到墙就整队停住；
    /// 队尾三颗蓝色的逃跑球停在进门那条道上。不关门就点：三颗蓝球顺着原路冲出门去，跑了。
    /// 等整队都进了笼子，把门关上再点：蓝球冲到门口被门挡住，正好停在队头旁边，一起烧掉。没有时机压力。
    ///
    /// 旧版（原第 7 关原样）是队头撞到笼子后墙、每颗球各自朝随机方向弹开，在笼子里乱撞。
    /// 同一套解法连跑 8 次只过 5 次，有一次不关门也过了 —— 过不过看运气。现在没有随机。
    /// 火圈放大到 1.7（够得着 1.03）：蓝球停在门口，离拐回来的队头 0.8，要够得着。
    /// 路在笼子里的长度正好比队伍长半颗球：队尾的蓝球就停在门口里面一点，身后只有门。
    /// （第一稿路长了一截，蓝球停在笼子中间，点笼子正中时它们往门口逃、正好穿过火圈，不关门也全中。）
    /// 还有一种不关门的解：把火圈点在门口，蓝球逃出来时自己撞进去。这是看懂了"它们往哪逃"才想得到的，留着。
    /// </summary>
    static LevelDef Net()
    {
        var d = SchoolLevel("net", "收网", "The Net", 10, BallPalette.HazySummer);
        d.ReactionMaxScale = 1.7f;
        // 笼子转 90°：门洞朝下，开在底边正中。进门的道是 x = 0，拐回来的道是 x = 0.8，顶到门洞右边那截墙
        var cage = new CageDef(0f, 2.3f, 3.4f, 2.9f, 90f, true, false);
        d.Cages.Add(cage);
        var s = new SchoolDef
        {
            Waypoints = new[]
            {
                new Vector2(0f, -9.6f), new Vector2(0f, 3.0f), new Vector2(0.8f, 3.0f), new Vector2(0.8f, 0.6f),
            },
            Loop = false, Count = 10, Escape = 3, Wave = false, Speed = 2.0f, MaxScale = 1.7f,
            HoldWhenBlocked = true,
        };
        d.Schools.Add(s);
        d.Doors.Add(new DoorDef(0f, 0.6f, 0f, startsOpen: true));
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
    /// 定向的分裂球，两颗火星朝 forwardDeg 左右各偏 40° 飞（和"背着火源飞"时从 forwardDeg 的反方向点着是同一个 V 字），
    /// 但方向是固定的，所以会画出瞄准虚线。
    /// </summary>
    static BallDef AimedV(Vector2 pos, float forwardDeg)
    {
        return BallDef.Aimed(pos.x, pos.y, forwardDeg + Ball.SplitHalfAngleDeg, forwardDeg - Ball.SplitHalfAngleDeg);
    }

    /// <summary>角度转单位向量</summary>
    static Vector2 Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));

    /// <summary>
    /// 样关 · 火种：底下一截竖直的引线通到五彩球。点引线最下面，火往上烧到五彩球，
    /// 炸出两颗火星，像喷泉一样分成两股飞向上方左右两条短线。教"五彩球会把火带到远处"。
    /// </summary>
    static LevelDef SampleSpark()
    {
        var d = new LevelDef { Id = "spark", NameZh = "火种", NameEn = "Spark",
            Palette = BallPalette.HazySummer, Sample = true };
        LevelDef.Line(d.Balls, new Vector2(0f, -4.4f), new Vector2(0f, -1.6f), 5);
        var split = new Vector2(0f, -0.8f);
        d.Balls.Add(AimedV(split, 90f));
        foreach (float sign in new[] { 1f, -1f })
        {
            var c = split + SparkDir(90f, sign) * 4.5f;
            LevelDef.Line(d.Balls, new Vector2(c.x - 1.4f, c.y), new Vector2(c.x + 1.4f, c.y), 5);
        }
        return d;
    }

    /// <summary>
    /// 样关 · 回火：底下一大团，一截桥往上通到五彩球。第一反应是点大团，可火星会背着火源往上飞，上面什么都没有。
    /// 要想通：点五彩球的上方，让火星往下飞到大团左右两边的两小团，五彩球自己的爆炸再顺桥烧回大团。
    /// </summary>
    static LevelDef SampleBackfire()
    {
        var d = new LevelDef { Id = "backfire", NameZh = "回火", NameEn = "Backfire",
            Palette = BallPalette.ShibuyaNight, Sample = true, Puzzle = true };
        LevelDef.Cluster(d.Balls, new Vector2(0f, -3.2f), 0.9f, 9);
        LevelDef.Line(d.Balls, new Vector2(0f, -2.0f), new Vector2(0f, -0.5f), 3);
        var split = new Vector2(0f, 0.3f);
        d.Balls.Add(new BallDef(split.x, split.y, BallKind.Split));
        foreach (float sign in new[] { 1f, -1f })
            LevelDef.Cluster(d.Balls, split + SparkDir(270f, sign) * 4.8f, 0.55f, 6);
        return d;
    }

    /// <summary>
    /// 样关 · 放行：一队球被闸拦在左下；右上一截引线通到五彩球，一颗火星近乎竖直地落到队伍的路上，另一颗飞向右边的小团。
    /// 开闸放队伍走，再掐准时间点引线：火星落下来那一刻正好砸在队头上，整队跟着亮。
    /// 开早了、开晚了火星都落在空路上；不开闸队伍停在远处够不着。门不是"先开好"的前置步骤，而是时机本身。
    /// </summary>
    static LevelDef SampleRelease()
    {
        var d = new LevelDef { Id = "release", NameZh = "放行", NameEn = "Release",
            Palette = BallPalette.Tiffany, Sample = true };
        var split = new Vector2(0.5f, 1.6f);
        var feed = Dir(120f);
        LevelDef.Line(d.Balls, split + feed * 0.8f, split + feed * 3.2f, 4);
        d.Balls.Add(AimedV(split, -60f));
        // 火从 120° 方向来，火星背着它飞：-60° ± 40° = -20°（去右边小团）和 -100°（落到路上 x ≈ -0.2）
        LevelDef.Cluster(d.Balls, split + SparkDir(-60f, 1f) * 5.85f, 0.5f, 6);
        // 起点再往左一点，队头一出生就停在闸前而不是嵌在门里；走慢些，火星砸队头的窗口才有半秒多
        var s = SchoolDef.Line(new Vector2(-10.0f, -2.5f), new Vector2(9.6f, -2.5f), 10);
        s.HoldWhenBlocked = true; s.Wave = false; s.Speed = 1.0f;
        d.Schools.Add(s);
        d.Walls.Add(new WallDef(-5.0f, -0.4f, 0.3f, 1.8f));
        d.Walls.Add(new WallDef(-5.0f, -4.35f, 0.3f, 1.3f));
        d.Doors.Add(new DoorDef(-5.0f, -2.5f, 90f) { Size = new Vector2(2.4f, 0.37f) });
        return d;
    }

    // =====================================================================
    // 样关：硬球
    // =====================================================================

    /// <summary>
    /// 样关 · 硬壳：一圈 20 颗球，顶上那颗是硬球，硬球外面再接一小截线（只有硬球炸了火才过得去）。
    /// 硬球要被火打两次：第一次打掉壳，要隔一会儿再来一把火才炸。
    /// 点在硬球旁边：近的那头立刻打掉壳，另一头的火绕一整圈回来再打一次 —— 全亮。
    /// 点在正对面：两头的火同时到，只打掉壳；点外面那截线：火从外面打掉壳，也没有第二把。
    /// </summary>
    static LevelDef SampleShell()
    {
        var d = new LevelDef { Id = "shell", NameZh = "硬壳", NameEn = "Hard Shell", Target = 0,
            Palette = BallPalette.Emerald, Sample = true, Puzzle = true, ExpandSpeed = 2.5f };
        const float r = 2.7f;
        LevelDef.Ring(d.Balls, new Vector2(0f, -0.6f), r, 20, 90f);
        var top = d.Balls[0]; top.Kind = BallKind.Hard; d.Balls[0] = top;
        LevelDef.Line(d.Balls, new Vector2(0f, -0.6f + r + 0.85f), new Vector2(0f, -0.6f + r + 2.55f), 3);
        return d;
    }

    // =====================================================================
    // 样关：联动门
    // =====================================================================

    /// <summary>
    /// 样关 · 分岔：火从右边烧到正中的五彩球，火星固定一上一下飞。上路近、门开着；下路远、门关着；两扇门联动。
    /// 上面的火星过了门再点一下门，下面的火星才赶得上开门。联动门把"先把门都开好"变成了"在飞行中切换"。
    /// </summary>
    static LevelDef SampleFork()
    {
        var d = new LevelDef { Id = "fork", NameZh = "分岔", NameEn = "Fork",
            Palette = BallPalette.TealOrange, Sample = true };
        // 五彩球放在偏上的位置：上门近、下门远，两颗火星先后到门的时间差拉到 0.9 秒（原来半秒，手忙脚乱）
        const float y = 1.6f;
        LevelDef.Cluster(d.Balls, new Vector2(5.6f, y), 0.7f, 6);
        LevelDef.Line(d.Balls, new Vector2(4.2f, y), new Vector2(0.9f, y), 5);
        d.Balls.Add(new BallDef(0f, y, BallKind.Split) { SplitAxisDeg = 90f });
        LevelDef.Line(d.Balls, new Vector2(-1.4f, 4.4f), new Vector2(1.4f, 4.4f), 5);
        LevelDef.Cluster(d.Balls, new Vector2(0f, -4.4f), 0.5f, 5);
        d.Doors.Add(new DoorDef(0f, y + 0.9f, 0f, startsOpen: true, link: 1));
        d.Doors.Add(new DoorDef(0f, -3.4f, 0f, startsOpen: false, link: 1));
        return d;
    }

    // =====================================================================
    // 引导关：分裂球 + 门。每关只教一件事，排在 放行 和 分岔 之间（理由见关卡表）
    // =====================================================================

    /// <summary>
    /// 引导 · 弹回：顶上一团、一截桥竖直往下通到五彩球。右下那颗火星直飞右下的小团；
    /// 左下那颗撞上一扇近乎水平的关着的门，被弹成朝左上飞，落进左边那一团。
    /// 什么都不用操作，点顶上那团就全亮 —— 只为了让玩家亲眼看见"火星会被门弹回，而且弹回来还有用"。
    /// 门转 5°（法线 95°）：火星以 230° 入射、45° 碰上门面、反射成 140°。
    /// </summary>
    static LevelDef GuideBounce()
    {
        var d = new LevelDef { Id = "guide-bounce", NameZh = "弹回", NameEn = "Bounce",
            Palette = BallPalette.HazySummer, Pool = LevelPool.Candidate };
        LevelDef.Cluster(d.Balls, new Vector2(0f, 3.8f), 0.7f, 6);
        LevelDef.Line(d.Balls, new Vector2(0f, 2.4f), new Vector2(0f, 0.9f), 3);
        var split = new Vector2(0f, 0.1f);
        d.Balls.Add(AimedV(split, 270f));
        LevelDef.Cluster(d.Balls, split + SparkDir(270f, 1f) * 4.4f, 0.55f, 6);      // 310°，直达
        var door = split + SparkDir(270f, -1f) * 2.2f;                                // 230°，撞门
        d.Doors.Add(new DoorDef(door.x, door.y, 5f));
        LevelDef.Cluster(d.Balls, door + Dir(140f) * 2.8f, 0.55f, 6);                 // 弹回后的落点
        return d;
    }

    /// <summary>
    /// 引导 · 改道：火从右边来。左上那颗火星直达左上的小团；左下那颗路上有扇近乎竖直的门，
    /// 一开始开着 —— 火星穿过去飞到空地，什么也没有。把门关上（点火前关，或点火后半秒内关），
    /// 火星才被弹成朝右下飞，落进下面那团。教"关门也是工具"。门转 -95°（法线 -5°）：220° 入射反射成 310°。
    /// </summary>
    static LevelDef GuideReroute()
    {
        var d = new LevelDef { Id = "guide-reroute", NameZh = "改道", NameEn = "Reroute",
            Palette = BallPalette.Tiffany, Pool = LevelPool.Candidate };
        LevelDef.Cluster(d.Balls, new Vector2(6.0f, 0.5f), 0.7f, 6);
        LevelDef.Line(d.Balls, new Vector2(4.6f, 0.5f), new Vector2(2.5f, 0.5f), 4);
        var split = new Vector2(1.6f, 0.5f);
        d.Balls.Add(AimedV(split, 180f));
        LevelDef.Cluster(d.Balls, split + SparkDir(180f, -1f) * 4.4f, 0.55f, 6);     // 140°，直达
        var door = split + SparkDir(180f, 1f) * 2.0f;                                 // 220°，门开着
        d.Doors.Add(new DoorDef(door.x, door.y, -95f, startsOpen: true));
        LevelDef.Cluster(d.Balls, door + Dir(310f) * 2.8f, 0.55f, 6);                 // 关门后的落点
        return d;
    }

    /// <summary>
    /// 引导 · 预备：斜着来。左下一团，桥沿 30° 往右上通到五彩球。上路那颗火星（70°）撞上一扇正对着它的关着的门，要开；
    /// 下路那颗（-10°）路上有扇开着的门，要关，关上它火星才被折成竖直向下、落进底下那团。
    /// 两件事点火前都能准备好，没有时机压力。这是 分岔 的前一步：分岔把两扇门联动起来，就只能在飞行中切了。
    /// </summary>
    static LevelDef GuidePrepare()
    {
        var d = new LevelDef { Id = "guide-prepare", NameZh = "预备", NameEn = "Prepare",
            Palette = BallPalette.Emerald, Pool = LevelPool.Candidate };
        var c0 = new Vector2(-6.0f, -3.4f);
        var u = Dir(30f);
        LevelDef.Cluster(d.Balls, c0, 0.7f, 6);
        LevelDef.Line(d.Balls, c0 + u * 1.4f, c0 + u * 3.8f, 4);
        var split = c0 + u * 4.6f;
        d.Balls.Add(AimedV(split, 30f));
        var upDoor = split + SparkDir(30f, 1f) * 2.0f;
        d.Doors.Add(new DoorDef(upDoor.x, upDoor.y, -20f));                          // 关着，法线正对火星
        LevelDef.Cluster(d.Balls, split + SparkDir(30f, 1f) * 4.6f, 0.55f, 6);
        var downDoor = split + SparkDir(30f, -1f) * 2.4f;
        d.Doors.Add(new DoorDef(downDoor.x, downDoor.y, 125f, startsOpen: true));    // 开着，要关；法线 215°：-10° 反射成 -100°
        LevelDef.Cluster(d.Balls, downDoor + Dir(-100f) * 2.6f, 0.5f, 5);
        return d;
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
    /// 漂流：一团六颗球整体以同一速度在一个框里漂流，撞墙一起反弹（弹性碰撞保形，队形永远不散）；
    /// 框中间竖着一条静止的线。趁那团正穿过线的时候点线，线和团一起亮。每四秒左右穿一次。
    /// 用的是旧「漂流」关的动法 —— 那关两个角落各一团，一下只够得着一团，做不到全清，所以撤了。
    /// </summary>
    static LevelDef Drift()
    {
        // 那团开场在线的右边、朝远离线的方向漂：先撞一次右墙再回来，第一次穿线在开场 3 秒左右，之后每 3.9 秒穿一次。
        // 以前它一开场就朝线冲过去，进关马上点线也能过（随手一点 6%）；现在得看着它漂、等它回来。
        // 框加宽到 12（原来 9），不然它来回太快，开场那一下还是蒙得上。
        // 火烧得短（0.6 秒）：烧 1.5 秒时提前两秒点线等它都能过，等于不用卡时机。
        // 那团必须比火胀得快（速度 3.1 > 2.5）：试过慢到 2.4，它过了线以后火还追得上，窗口反而更宽。
        var d = new LevelDef { Id = "drift", NameZh = "漂流", NameEn = "Drift",
            Palette = BallPalette.Tiffany, Pool = LevelPool.Candidate, ReactionDuration = 0.6f };
        d.Walls.Add(new WallDef(0f, 3.4f, 12.3f, 0.3f));
        d.Walls.Add(new WallDef(0f, -3.4f, 12.3f, 0.3f));
        d.Walls.Add(new WallDef(-6.0f, 0f, 0.3f, 7.1f));
        d.Walls.Add(new WallDef(6.0f, 0f, 0.3f, 7.1f));
        LevelDef.Line(d.Balls, new Vector2(0f, -2.8f), new Vector2(0f, 2.8f), 9);
        var mover = new List<BallDef>();
        LevelDef.Cluster(mover, new Vector2(1.6f, -1.0f), 0.55f, 5);
        foreach (var m in mover) { var b = m; b.Vel = new Vector2(2.6f, 1.7f); d.Balls.Add(b); }
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

    /// <summary>
    /// 流星：一颗五彩球在两根柱子之间横着来回飞，底下一条竖线伸到它的航线下面一点点。
    /// 只有它经过线顶的那一刻（约半秒）点线顶，火才够得着它；火星固定沿 60° 正反飞，一颗到右上的小团，一颗到左下的小团。
    /// 点线的下半截也行，但要提前点 —— 火往上爬要一秒多。
    /// </summary>
    static LevelDef Meteor()
    {
        // 火烧得短一点（0.9 秒）：不然把火圈点在它的航线上等它撞进来也能过，随手一点 10%；缩短后只剩真正卡时机的那半秒
        var d = new LevelDef { Id = "meteor", NameZh = "流星", NameEn = "Meteor",
            Palette = BallPalette.Rainbow, Pool = LevelPool.Candidate, ReactionDuration = 0.9f };
        LevelDef.Line(d.Balls, new Vector2(0f, -4.4f), new Vector2(0f, -0.2f), 7);
        // 开场在右半边、朝右飞：先撞一次柱子再回来，第一次经过线顶在开场 3 秒左右。
        // 以前它一开场就朝线顶飞过来，进关马上点线顶等它也能过。
        var m = new BallDef(1.0f, 0.6f, BallKind.Split) { SplitAxisDeg = 60f };
        m.Vel = new Vector2(2.0f, 0f);
        d.Balls.Add(m);
        d.Walls.Add(new WallDef(4.0f, 0.6f, 0.3f, 1.2f));
        d.Walls.Add(new WallDef(-4.0f, 0.6f, 0.3f, 1.2f));
        LevelDef.Cluster(d.Balls, new Vector2(2.0f, 4.06f), 0.5f, 5);
        LevelDef.Cluster(d.Balls, new Vector2(-2.0f, -2.86f), 0.5f, 5);
        return d;
    }

    /// <summary>
    /// 回旋：正中一颗五彩球，外面一圈六颗；左右上方两面斜墙，底下两团。
    /// 火星背着火源飞，所以从哪边点决定它往哪飞：只有从正下方点（先点着最下面那颗），
    /// 火星才以 50° / 130° 飞出、撞上斜墙折成竖直向下、落进底下两团。从上面或侧面点，火星飞到空地。
    /// 墙：右墙长边沿 340°（rot -110°，法线 250°），左墙长边沿 20°（rot -70°，法线 290°），火星离法线 20° 碰上墙面，不会擦到端头。
    /// </summary>
    static LevelDef Boomerang()
    {
        var d = new LevelDef { Id = "boomerang", NameZh = "回旋", NameEn = "Boomerang",
            Palette = BallPalette.TealOrange, Pool = LevelPool.Candidate, Puzzle = true };
        d.Balls.Add(new BallDef(0f, 0f, BallKind.Split));
        LevelDef.Ring(d.Balls, Vector2.zero, 0.7f, 6, 90f);
        d.Walls.Add(new WallDef(1.29f, 1.53f, 0.3f, 1.8f, -110f));
        d.Walls.Add(new WallDef(-1.29f, 1.53f, 0.3f, 1.8f, -70f));
        LevelDef.Cluster(d.Balls, new Vector2(1.29f, -3.0f), 0.55f, 6);
        LevelDef.Cluster(d.Balls, new Vector2(-1.29f, -3.0f), 0.55f, 6);
        return d;
    }

    // =====================================================================
    // 备选：硬球
    // =====================================================================

    /// <summary>
    /// 双锁：正中一颗定向的五彩球，火星左右各飞向一颗硬球（瞄准虚线看得见）。
    /// 左边是一道矮拱桥，火沿着桥烧到左锁和火星飞到左锁几乎同时 —— 两把火撞在一起，只碎壳；
    /// 右边是一道高高的门形长路，火星先到、桥火晚一秒半才到 —— 右锁开得了。
    /// 所以点五彩球（最顺手的地方）只开右锁。要想通：从左锁那头点，让桥火先敲碎左锁，
    /// 火烧到五彩球再飞回来的火星才是第二下；右锁照旧是火星先到、长路后到。
    /// 两颗锁后面各挂一团（在火星航线下面，火星碰不到）：锁不开，那团就不亮。
    /// 点右锁那头：右锁开，左锁还是撞车。点桥顶、点那两团都不行。
    /// </summary>
    static LevelDef TwoLocks()
    {
        var d = new LevelDef { Id = "two-locks", NameZh = "双锁", NameEn = "Two Locks",
            Palette = BallPalette.Emerald, Pool = LevelPool.Candidate, Puzzle = true };
        d.Balls.Add(BallDef.Aimed(0f, 0f, 0f));
        // 左：矮拱桥，五颗
        float[] bx = { 0.6f, 1.3f, 2.05f, 2.8f, 3.45f };
        float[] by = { 0.5f, 0.78f, 0.9f, 0.78f, 0.48f };
        for (int i = 0; i < bx.Length; i++) d.Balls.Add(new BallDef(-bx[i], by[i]));
        // 右：门形长路，上去、横过去、再下来
        Path(d.Balls, 0.8f, new Vector2(0.55f, 0.6f), new Vector2(0.55f, 3.6f), new Vector2(1.15f, 4.2f),
            new Vector2(3.3f, 4.2f), new Vector2(3.9f, 3.6f), new Vector2(3.9f, 0.8f));
        foreach (float sx in new[] { -1f, 1f })
        {
            d.Balls.Add(new BallDef(sx * 3.9f, 0f, BallKind.Hard));
            LevelDef.Cluster(d.Balls, new Vector2(sx * 4.3f, -1.35f), 0.7f, 6);
        }
        return d;
    }

    /// <summary>
    /// 钥匙：一把伞。伞柄是一截引线，通到伞柄顶上那颗定向的五彩球；伞面是一道大半圆，右端是硬球，硬球下面挂一截尾巴。
    /// 两颗火星一左一右（虚线看得见）：右边那颗直接敲碎硬球的壳；左边那颗点着伞面的左端，
    /// 火沿着伞面绕一大圈，两秒后从上面烧到硬球 —— 这是第二下，硬球炸，尾巴跟着亮。
    /// 教"火星也算一把火，而且两颗火星可以各干各的"。点引线或五彩球就过。
    /// 点伞面：火烧到硬球只碎壳，五彩球根本没着；点尾巴、点硬球也一样 —— 都看得出差在"五彩球没点着"。
    ///
    /// 为什么不让伞面连着五彩球：试过马蹄形（五彩球—长链—硬球，火星抄近路）。火星和链火会在链上某一段
    /// "同时到"（差不到一秒），点在那一段就只差硬球一颗，玩家看不出为什么。只要两把火都从同一条链上出发，
    /// 这段一秒宽的死区就躲不掉。现在两把火的先后只由摆法决定，和点哪里无关。
    /// 旧版（左团 + 直线 + 硬球 + 右团）的毛病：火星贴着那条线飞、一路把线点着，又穿过硬球把右团点了，
    /// 硬球自己却常常留下；能过的那种点法也只靠 0.07 秒的余量 —— 用户说的"有点奇怪"。
    /// </summary>
    static LevelDef Key()
    {
        var d = new LevelDef { Id = "key", NameZh = "钥匙", NameEn = "Key",
            Palette = BallPalette.ShibuyaNight, Pool = LevelPool.Candidate };
        var hub = new Vector2(-1.5f, -0.6f);
        const float r = 3.5f;
        LevelDef.Line(d.Balls, hub + new Vector2(0f, -2.4f), hub + new Vector2(0f, -0.8f), 3);
        d.Balls.Add(BallDef.Aimed(hub.x, hub.y, 0f));
        // 伞面：从正左（火星落点）绕过顶到右边，最后一格留给硬球
        const int n = 14;
        for (int k = 0; k < n; k++)
        {
            float a = (180f - 180f * k / n) * Mathf.Deg2Rad;
            d.Balls.Add(new BallDef(hub.x + Mathf.Cos(a) * r, hub.y + Mathf.Sin(a) * r));
        }
        d.Balls.Add(new BallDef(hub.x + r, hub.y, BallKind.Hard));
        LevelDef.Line(d.Balls, new Vector2(hub.x + r, hub.y - 0.8f), new Vector2(hub.x + r, hub.y - 3.2f), 4);
        return d;
    }

    /// <summary>
    /// 门闩：队伍走到闸前停下（开场约 2.5 秒到）；闸后紧挨着一颗硬球，硬球挡着一条线。
    /// 点硬球和线之间的缝：壳碎了，线烧起来，但线火和敲壳是同一波，硬球不炸。
    /// 等一拍再开闸（点缝之后约 0.6–1.8 秒）：队头走进还没散的火圈里着火，它是新的一把火，正好贴着硬球 —— 门闩断了，整队跟着亮。
    /// 开了闸马上点：队头到硬球跟前时才着火，和敲壳是同一波，硬球留下。点了缝不开闸：硬球裂着，队伍停着。
    /// 另一种解：闸一直开着，趁队头离硬球还有两步远时点缝，队头走进火圈时壳已经碎了一秒多。
    /// 这关的火烧得久一点（2.2 秒），窗口才够宽。
    /// </summary>
    static LevelDef Bolt()
    {
        var d = SchoolLevel("bolt", "门闩", "Bolt", 0, BallPalette.HazySummer);
        d.Pool = LevelPool.Candidate; d.Puzzle = true;
        d.ReactionDuration = 2.2f;
        var s = SchoolDef.Line(new Vector2(-9.6f, 0f), new Vector2(9.6f, 0f), 10);
        s.HoldWhenBlocked = true; s.Wave = false; s.Duration = 2.2f;
        d.Schools.Add(s);
        d.Walls.Add(new WallDef(-1.0f, 2.6f, 0.3f, 2.8f));
        d.Walls.Add(new WallDef(-1.0f, -2.6f, 0.3f, 2.8f));
        d.Doors.Add(new DoorDef(-1.0f, 0f, 90f) { Size = new Vector2(2.4f, 0.37f) });
        d.Balls.Add(new BallDef(-0.3f, 0f, BallKind.Hard));
        LevelDef.Line(d.Balls, new Vector2(0.4f, 0f), new Vector2(3.2f, 0f), 5);
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
        foreach (var s in new[] { a, b }) { s.HoldWhenBlocked = true; s.Wave = false; s.ShowPath = true; d.Schools.Add(s); }
        // 两扇门都在圈的右边：上队（向右）停下时队身横过圈；下队（向左）在圈右边等，一放就进圈
        d.Doors.Add(new DoorDef(2.0f, 1.2f, 90f, startsOpen: true, link: 1));
        d.Doors.Add(new DoorDef(2.0f, -1.2f, 90f, startsOpen: false, link: 1));
        LevelDef.Ring(d.Balls, Vector2.zero, 0.9f, 8, 90f);
        return d;
    }

    /// <summary>
    /// 瓮中：一队球从左边走进右边的笼子，在里面掉个头，又从同一个门洞走出来（路线画着，一去一回两条道）。
    /// 整队都在笼子里的时间只有一秒左右：队尾刚进门、队头还没出门的那一刻把门关上，
    /// 队头被门拦住，整队停在笼子里；这时再点，队尾四颗逃跑球往门口冲、被门挡在队头旁边，全亮。
    /// 关早了：队尾被关在门外，撞门弹开；关晚了：队头已经出去了。门可以反复开关，队头被拦在门外时再开它就接着走。
    ///
    /// 上一版让蛇盘进笼子、走到路的尽头停住。两个毛病：路不循环时后面的球会一直挤到尽头叠成一个点，
    /// 等上十秒随便点就全中；逃跑球逃的方向不对着门，关不关门都一样（自动测试：不关门 10/10）。
    /// </summary>
    static LevelDef Jar()
    {
        var d = SchoolLevel("jar", "瓮中", "In the Jar", 0, BallPalette.HazySummer);
        d.Pool = LevelPool.Candidate;
        const float lane = 0.3f;
        var s = new SchoolDef
        {
            Waypoints = new[]
            {
                new Vector2(-9.6f, lane), new Vector2(5.0f, lane), new Vector2(5.0f, -lane), new Vector2(-9.6f, -lane),
            },
            Loop = true, Count = 10, Escape = 4, Wave = false, Speed = 1.7f, HoldWhenBlocked = true,
        };
        d.Schools.Add(s);
        d.Cages.Add(new CageDef(3.6f, 0f, 3.6f, 2.4f, 0f, true, false));
        d.Doors.Add(new DoorDef(1.8f, 0f, 90f, startsOpen: true));
        return d;
    }

    // =====================================================================
    // 备选：奇观
    // =====================================================================

    /// <summary>螺旋：阿基米德螺线，圈距 1.25（够不着），火只能顺着线转圈走</summary>
    static LevelDef Spiral()
    {
        var d = new LevelDef { Id = "spiral", NameZh = "螺旋", NameEn = "Spiral", Target = 0,
            Palette = BallPalette.Tiffany, ExpandSpeed = 5f, Pool = LevelPool.Candidate };
        // 间距 0.65、膨胀 5/秒：每跳 0.1 秒，从中心点整条线 8 秒左右烧完（原来 3/秒要 15 秒，用户嫌放水关等得久）
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
            Palette = BallPalette.Emerald, ExpandSpeed = 5f, Pool = LevelPool.Candidate };
        // 行内间距 0.7、膨胀 5/秒：每跳 0.12 秒，从一头点 12 秒烧完、从中间点 6 秒（原来 3/秒要 22 秒）
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

    /// <summary>沿一条折线按大致 spacing 的间距摆球，两个端点都放（间距取整后均分）</summary>
    static void Path(List<BallDef> into, float spacing, params Vector2[] pts)
    {
        float total = 0f;
        for (int i = 1; i < pts.Length; i++) total += Vector2.Distance(pts[i - 1], pts[i]);
        int n = Mathf.Max(1, Mathf.RoundToInt(total / spacing));
        for (int k = 0; k <= n; k++)
        {
            float want = total * k / n;
            Vector2 p = pts[pts.Length - 1];
            float acc = 0f;
            for (int i = 1; i < pts.Length; i++)
            {
                float seg = Vector2.Distance(pts[i - 1], pts[i]);
                if (acc + seg >= want - 0.0001f)
                {
                    p = Vector2.Lerp(pts[i - 1], pts[i], seg < 0.0001f ? 0f : (want - acc) / seg);
                    break;
                }
                acc += seg;
            }
            into.Add(new BallDef(p.x, p.y));
        }
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
