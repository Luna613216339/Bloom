using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 球的种类。Normal 是普通球；Split 是分裂球（五彩，被波及后裂成两颗火星）；
/// Hard 是硬球（有外壳，要被两处不同的火各碰一次才引爆）；
/// Fragment 是分裂球裂出来的火星，只在运行时出现，关卡数据里不写它。
/// </summary>
public enum BallKind { Normal, Split, Hard, Fragment }

/// <summary>
/// 一面墙：轴对齐或带旋转的矩形，球撞上会弹回。爆炸不认墙（连锁检测是距离判断，穿墙）。
/// </summary>
public struct WallDef
{
    public Vector2 Center;
    public Vector2 Size;
    public float Rot;
    public WallDef(float x, float y, float w, float h, float rot = 0f)
    {
        Center = new Vector2(x, y); Size = new Vector2(w, h); Rot = rot;
    }
}

/// <summary>
/// 一扇门。Link 不为 0 的门是联动门：点其中一扇，同组的每一扇都翻转一次，
/// 所以初始一开一关的两扇门永远一开一关。
/// </summary>
public class DoorDef
{
    public Vector2 Center;
    public Vector2 Size = new Vector2(1.2120601f, 0.37303004f);   // 旧关卡里门的实际尺寸
    public float Rot;
    public bool StartsOpen;
    public int Link;
    public Color Color = DefaultColor;

    public static readonly Color DefaultColor = new Color(0.26415092f, 0.088465646f, 0.088465646f);
    public static readonly Color LinkedColor = new Color(0.05f, 0.42f, 0.47f);

    public DoorDef(float x, float y, float rot, bool startsOpen = false, int link = 0)
    {
        Center = new Vector2(x, y); Rot = rot; StartsOpen = startsOpen; Link = link;
        if (link != 0) Color = LinkedColor;
    }
}

/// <summary>笼子：四面墙，左右两侧可以留门洞（门洞里要不要放门是 Doors 的事）</summary>
public class CageDef
{
    public Vector2 Center;
    public float Width = 3f, Height = 4f, Rot;
    public float DoorGap = 1.2f;
    public bool LeftDoor = true, RightDoor = true;
    public float Thickness = 0.38f;
    public Color Color = new Color(0.21960784f, 0.21960784f, 0.21960784f);

    public CageDef(float x, float y, float w, float h, float rot, bool left, bool right)
    {
        Center = new Vector2(x, y); Width = w; Height = h; Rot = rot; LeftDoor = left; RightDoor = right;
    }
}

/// <summary>
/// 一队沿路径走的球（蛇）。路径是折线（Waypoints，世界坐标）或圆（Circle）。
/// 队尾最后 Escape 颗是逃跑球。
/// </summary>
public class SchoolDef
{
    public Vector2[] Waypoints;
    public bool Loop = true;

    public bool Circle;
    public Vector2 Center;
    public float Radius = 2.5f;
    public bool Clockwise;
    public float PhaseDeg;          // 圆路径的起始相位（对应旧场景里 transform 的 z 旋转）

    public int Count = 8;
    public float Spacing = 0.5f;
    public float Speed = 1.5f;
    public float StartOffset;       // 额外的起始路程，用来错开两队球的相位
    public bool Wave = true;
    public float WaveAmplitude = 0.3f;
    public float WaveSpeed = 3f;
    public int Escape;
    public float DangerRadius = 2f;
    public float FleeSpeed = 6f;

    public float BallSize = 0.35f;
    public float MaxScale = 1.3f;
    public float Duration = 1.5f;
    public float ExpandSpeed = 1.5f;

    /// <summary>队头撞到关着的门时整队停下等（true），还是像旧关那样各自散开（false）</summary>
    public bool HoldWhenBlocked;
    public bool ShowPath;
    public Color[] Palette;         // 不填就用关卡配色
    public BallKind[] Kinds;        // 按队列索引指定特殊球，不填全是普通球

    public static SchoolDef Line(Vector2 a, Vector2 b, int count)
    {
        return new SchoolDef { Waypoints = new[] { a, b }, Count = count };
    }

    public static SchoolDef Orbit(Vector2 center, float radius, int count, bool clockwise, float phaseDeg)
    {
        return new SchoolDef
        {
            Circle = true, Center = center, Radius = radius, Count = count,
            Clockwise = clockwise, PhaseDeg = phaseDeg, Wave = false,
        };
    }
}

/// <summary>一颗摆好位置的球。Vel 为零就是静止的</summary>
public struct BallDef
{
    public Vector2 Pos;
    public Vector2 Vel;
    public BallKind Kind;
    public float Size;              // 0 = 用关卡默认
    public float SplitAxisDeg;      // 分裂球专用：火星固定沿这个方向正反飞；NaN = 背着火源飞
    public BallDef(float x, float y, BallKind kind = BallKind.Normal)
    {
        Pos = new Vector2(x, y); Vel = Vector2.zero; Kind = kind; Size = 0f; SplitAxisDeg = float.NaN;
    }
}

/// <summary>
/// 一片随机散布的球。Seed 小于 0 表示每次进关重新随机；否则布局固定，适合解谜关。
/// </summary>
public class ScatterDef
{
    public int Count = 20;
    public int Seed = -1;
    public Rect Region = FullField;
    public float Speed = -1f;       // 小于 0 = 用关卡默认
    public BallKind Kind = BallKind.Normal;

    public const float HalfW = 5f * 16f / 9f;
    public const float HalfH = 5f;
    public static readonly Rect FullField = new Rect(-HalfW + 0.5f, -HalfH + 0.5f, 2f * HalfW - 1f, 2f * HalfH - 1f);
}

/// <summary>
/// 一关的全部数据。关卡只在 LevelCatalog 里写，场景里不存任何关卡参数。
/// </summary>
public class LevelDef
{
    public int Number;              // 由 LevelCatalog 按顺序填，1 起
    public string Id;               // 英文短 id，日志和自动测试用
    public string NameZh;
    public string NameEn;
    public int Target;

    public Color[] Palette = BallPalette.HazySummer;
    public float BallSpeed = 1.8f;
    public float BallSize = 0.4f;
    public float ReactionMaxScale = 1.48f;
    public float ReactionDuration = 1.5f;
    public float ExpandSpeed = 5f;

    /// <summary>解谜关：布局固定、续命默认关掉（花钱多点一下会把谜题绕过去）</summary>
    public bool Puzzle;
    public bool AllowContinue = true;

    /// <summary>样关：依赖还没批准的机制，关卡表里单独标</summary>
    public bool Sample;

    public List<WallDef> Walls = new List<WallDef>();
    public List<DoorDef> Doors = new List<DoorDef>();
    public List<CageDef> Cages = new List<CageDef>();
    public List<SchoolDef> Schools = new List<SchoolDef>();
    public List<BallDef> Balls = new List<BallDef>();
    public List<ScatterDef> Scatter = new List<ScatterDef>();

    /// <summary>场上一共几颗算数的球（火星不算）</summary>
    public int TotalBalls
    {
        get
        {
            int n = Balls.Count;
            foreach (var s in Schools) n += s.Count;
            foreach (var s in Scatter) n += s.Count;
            return n;
        }
    }

    public bool HasRandomLayout
    {
        get
        {
            foreach (var s in Scatter) if (s.Seed < 0) return true;
            return false;
        }
    }

    // ---- 摆阵用的小工具 ----

    public static void Ring(List<BallDef> into, Vector2 c, float r, int n, float phaseDeg = 90f,
        BallKind kind = BallKind.Normal)
    {
        for (int i = 0; i < n; i++)
        {
            float a = (phaseDeg + 360f * i / n) * Mathf.Deg2Rad;
            into.Add(new BallDef(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r, kind));
        }
    }

    /// <summary>一圈 n 颗加中间一颗</summary>
    public static void Cluster(List<BallDef> into, Vector2 c, float r, int n, float phaseDeg = 90f)
    {
        into.Add(new BallDef(c.x, c.y));
        Ring(into, c, r, n, phaseDeg);
    }

    public static void Line(List<BallDef> into, Vector2 a, Vector2 b, int n, BallKind kind = BallKind.Normal)
    {
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : (float)i / (n - 1);
            var p = Vector2.Lerp(a, b, t);
            into.Add(new BallDef(p.x, p.y, kind));
        }
    }

    /// <summary>正弦波：从 x0 到 x1，按弧长大致等距摆 n 颗</summary>
    public static void SineWave(List<BallDef> into, float x0, float x1, float yCenter, float amp, float periods, int n)
    {
        // 先把曲线细分，再按弧长等距取点
        const int fine = 2000;
        var pts = new Vector2[fine + 1];
        var cum = new float[fine + 1];
        for (int i = 0; i <= fine; i++)
        {
            float t = (float)i / fine;
            float x = Mathf.Lerp(x0, x1, t);
            float y = yCenter + amp * Mathf.Sin(t * periods * Mathf.PI * 2f);
            pts[i] = new Vector2(x, y);
            cum[i] = i == 0 ? 0f : cum[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
        }
        float total = cum[fine];
        int k = 0;
        for (int i = 0; i < n; i++)
        {
            float want = total * i / (n - 1);
            while (k < fine && cum[k + 1] < want) k++;
            into.Add(new BallDef(pts[k].x, pts[k].y));
        }
    }
}
