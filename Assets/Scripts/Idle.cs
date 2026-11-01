using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 让一团静止的球"活着"：按 IdleDef 的动法小幅移动。正在烧的球也跟着团一起动 ——
/// 要是点着的那颗停在原地、旁边的还在胀缩，两颗球的距离就不再按比例变，链会断（涟漪实测过）。
/// 球是 Kinematic 的，用 MovePosition 推，连锁判定读的 transform 跟着走。
///
/// 五种动法：
///   Bob     整团上下浮
///   Sway    整团左右摆
///   Spin    整团绕自己的重心慢转（连续转，不是摆）
///   Breathe 整团微微胀缩（幅度是相对半径的比例，球距按同样比例变）
///   Drift   每颗各自轻微漂移（李萨如曲线，相位和频率各不相同）
/// </summary>
public class IdleGroup : MonoBehaviour
{
    struct Entry
    {
        public Ball ball;
        public Rigidbody2D rb;
        public Vector2 basePos;
        public Vector2 centroid;        // 所在那一团的重心
        public float phaseA, phaseB, wA, wB;
        public float scale;             // 呼吸幅度在这一团上打的折扣（见 SafeBreathe）
    }

    /// <summary>两颗球隔得比这近就算同一团（比连锁够得着的距离略大）</summary>
    const float LinkDistance = 1.0f;

    readonly List<Entry> entries = new List<Entry>();
    IdleDef def;
    float t;

    /// <summary>
    /// 静止的球按"挨得近"分成一团一团，每团绕自己的重心动。
    /// 半径超过 def.MaxGroupRadius 的团（长线、螺旋、大图案）不动 —— 用户试玩：只有圆形小团动起来好看。
    /// </summary>
    /// <param name="reach">两颗球中心隔多远以内火能传过去（膨胀后半径 + 对方半径）</param>
    public void Configure(IdleDef def, List<Ball> balls, float reach)
    {
        this.def = def;
        var rng = new System.Random(def.Seed);
        foreach (var group in Groups(balls))
        {
            Vector2 c = Vector2.zero;
            foreach (var b in group) c += (Vector2)b.transform.position;
            c /= group.Count;
            float r = 0.3f;
            foreach (var b in group) r = Mathf.Max(r, Vector2.Distance(b.transform.position, c));
            if (r > def.MaxGroupRadius) continue;
            float k = def.Mode == IdleMode.Breathe ? SafeBreathe(group, balls, reach, r) : 1f;
            foreach (var b in group)
            {
                entries.Add(new Entry
                {
                    ball = b,
                    rb = b.GetComponent<Rigidbody2D>(),
                    basePos = b.transform.position,
                    centroid = c,
                    phaseA = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                    phaseB = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                    wA = Mathf.Lerp(0.7f, 1.2f, (float)rng.NextDouble()),
                    wB = Mathf.Lerp(0.7f, 1.2f, (float)rng.NextDouble()),
                    scale = k,
                });
            }
        }
    }

    /// <summary>
    /// 呼吸会改变球距。有的团设计时球距离"够得着"只差 0.04（或者故意差一点点够不着），
    /// 胀缩一下链就断了 / 接上了。整团按比例 k 胀缩时，团内两颗球的距离变 k·d，团里一颗和团外一颗最多变 k·r；
    /// 离"够得着"最近的那一对决定这团最多胀缩多少，再留一半余量。返回给 def.Amplitude 打的折扣（0–1）。
    /// </summary>
    float SafeBreathe(List<Ball> group, List<Ball> all, float reach, float r)
    {
        var inGroup = new HashSet<Ball>(group);
        float kMax = float.MaxValue;
        foreach (var a in group)
            foreach (var b in all)
            {
                if (a == b) continue;
                float d = Vector2.Distance(a.transform.position, b.transform.position);
                if (d > reach * 2f) continue;
                float stretch = inGroup.Contains(b) ? d : r;
                kMax = Mathf.Min(kMax, 0.5f * Mathf.Abs(d - reach) / stretch);
            }
        return Mathf.Clamp01(kMax / Mathf.Max(0.0001f, def.Amplitude));
    }

    static List<List<Ball>> Groups(List<Ball> balls)
    {
        int n = balls.Count;
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;
        int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (Vector2.Distance(balls[i].transform.position, balls[j].transform.position) < LinkDistance)
                    parent[Find(i)] = Find(j);
        var map = new Dictionary<int, List<Ball>>();
        for (int i = 0; i < n; i++)
        {
            int root = Find(i);
            if (!map.TryGetValue(root, out var list)) map[root] = list = new List<Ball>();
            list.Add(balls[i]);
        }
        return new List<List<Ball>>(map.Values);
    }

    void FixedUpdate()
    {
        if (def == null || def.Mode == IdleMode.None) return;
        t += Time.fixedDeltaTime;
        float w = Mathf.PI * 2f / Mathf.Max(0.1f, def.Period);

        foreach (var e in entries)
        {
            if (e.ball == null) continue;
            var st = e.ball.CurrentState;
            if (st != Ball.BallState.Moving && st != Ball.BallState.Triggered) continue;
            Vector2 p = e.basePos;
            switch (def.Mode)
            {
                case IdleMode.Bob:
                    p += Vector2.up * (def.Amplitude * Mathf.Sin(w * t));
                    break;
                case IdleMode.Sway:
                    p += Vector2.right * (def.Amplitude * Mathf.Sin(w * t));
                    break;
                case IdleMode.Spin:
                {
                    float a = def.Amplitude * Mathf.Deg2Rad * t;
                    Vector2 r = e.basePos - e.centroid;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    p = e.centroid + new Vector2(r.x * c - r.y * s, r.x * s + r.y * c);
                    break;
                }
                case IdleMode.Breathe:
                {
                    float k = def.Amplitude * e.scale * Mathf.Sin(w * t);
                    p = e.basePos + (e.basePos - e.centroid) * k;
                    break;
                }
                case IdleMode.Drift:
                    p += new Vector2(Mathf.Sin(e.wA * w * t + e.phaseA), Mathf.Sin(e.wB * w * t + e.phaseB)) * def.Amplitude;
                    break;
            }
            e.rb.MovePosition(p);
        }
    }
}
