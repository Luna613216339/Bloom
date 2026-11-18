using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画方案（美术方案审核）：每一项除了 A（原来的代码）以外的版本都在这个文件。
/// 选哪一版看 Look.cs 里的 ClickFx / PopFx / CrackFx / DoorFx / IntroFx / WinFx / FailFx。
/// 第一轮没被选中的版本（逃跑 B/C、火星 B/C、点击圈 B/C、消失 C、碎壳 B/C、失败 B）已经删掉。
///
/// 规矩：这里只画东西，不碰判定和时间。所有效果都是另外生出来的物体，读球的状态、跟着画；
/// 不改球的 transform 缩放、碰撞体、状态机。球自己那张贴图最多被关掉显示（renderer.enabled）。
/// 用户选定以后，没选中的版本连同这里对应的函数可以整段删掉。
/// </summary>
public static class Fx
{
    static readonly List<GameObject> scene = new List<GameObject>();     // 换关时要清掉的
    static readonly List<GameObject> ending = new List<GameObject>();    // 结算画面上的（续命时清掉）
    static readonly List<(Vector2 pos, float size)> burned = new List<(Vector2, float)>();

    public static float OutCubic(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t) * (1f - t); }
    public static float InOut(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    public static float OutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// <summary>换关 / 重开：上一局留下的效果全部清掉</summary>
    public static void Reset()
    {
        foreach (var go in scene) if (go != null) Object.Destroy(go);
        scene.Clear();
        ClearEnding();
        burned.Clear();
        Afterglow.ClearAll();
    }

    /// <summary>结算画面上的标记（褪色、圈出）收掉。续命和重开时调</summary>
    public static void ClearEnding()
    {
        foreach (var go in ending) if (go != null) Object.Destroy(go);
        ending.Clear();
    }

    public static void NoteBurned(Vector2 pos, float size) => burned.Add((pos, size));

    static GameObject Track(GameObject go) { scene.Add(go); return go; }

    static SpriteRenderer Disc(string name, Vector2 pos, float diameter, Color color, int order)
    {
        var go = Track(new GameObject(name));
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * diameter;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Circle;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static RingLine Ring(string name, Vector2 pos, int order) => RingLine.Create(Track(new GameObject(name)), pos, order);

    static LineRenderer Line(string name, int order, float width, Color color, bool round = true)
    {
        var go = Track(new GameObject(name));
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = RingLine.Mat;
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = color;
        lr.numCapVertices = round ? 6 : 0;
        lr.sortingOrder = order;
        return lr;
    }

    // ================= 3.1 玩家点出的圈 =================
    // 第二轮：用户要 A 的形态（一片淡淡的圆盘）里面再加涟漪，不要有很实的边。
    //   D 圆盘里两道涟漪：点下去以后两圈细波先后从落点荡出去，快到边上就淡没了。
    //   E 涟漪不断：圆盘还在的时候，每隔一小会儿从落点再荡出一圈。
    //   F 只有涟漪：不画整片圆盘，圆盘本身就是几道很宽很淡的波，最外一道淡到看不出边。

    public static void Click(ClickReaction cr, SpriteRenderer sr)
    {
        if (!Look.On || Look.ClickFx == FxV.A) return;
        Vector2 pos = cr.transform.position;
        Color fire = Look.Fire;
        var mode = Look.ClickFx;

        if (mode == FxV.F)
        {
            sr.enabled = false;
            var bands = new RingLine[4];
            for (int i = 0; i < bands.Length; i++) bands[i] = Ring("ClickWave", pos, 2);
            FxRun.Go(bands[0].gameObject, age =>
            {
                if (cr == null)
                {
                    for (int i = 1; i < bands.Length; i++) Object.Destroy(bands[i].gameObject);
                    return false;
                }
                float R = cr.Scale * 0.5f, f = cr.Fade01;
                // 四道宽波一起撑满整个圆：越靠外越淡，每道波还在慢慢往外走，走到头从中心再出来
                for (int i = 0; i < bands.Length; i++)
                {
                    float u = Mathf.Repeat(i / (float)bands.Length + age * 0.22f, 1f);     // 0 = 圆心，1 = 边上
                    float w = R / bands.Length * 0.62f;
                    float a = 0.34f * (1f - u * u) * Mathf.Clamp01(u * 6f);
                    bands[i].Set(Mathf.Max(w * 0.5f, R * u), w, A(fire, a * f));
                }
                return true;
            });
            return;
        }

        // D / E：圆盘就是原来那张贴图（稍微淡一点，给涟漪让位），上面叠涟漪
        var c0 = sr.color;
        sr.color = A(c0, c0.a * 0.8f);
        int count = mode == FxV.D ? 2 : 12;
        float gap = mode == FxV.D ? 0.16f : 0.34f, speed = mode == FxV.D ? 1.25f : 0.95f;
        var rings = new RingLine[count];
        for (int i = 0; i < count; i++) rings[i] = Ring("ClickRipple", pos, 2);
        FxRun.Go(rings[0].gameObject, age =>
        {
            if (cr == null)
            {
                for (int i = 1; i < count; i++) Object.Destroy(rings[i].gameObject);
                return false;
            }
            float R = cr.Scale * 0.5f, f = cr.Fade01;
            for (int i = 0; i < count; i++)
            {
                float r = speed * (age - 0.06f - gap * i);
                // 离边越近越淡，到边之前就没了，所以圆盘的边上不会多出一道实线
                float k = R > 0.01f ? Mathf.Clamp01(r / R) : 1f;
                float a = r <= 0f || k >= 1f ? 0f : 0.5f * (1f - k) * (1f - k) * Mathf.Clamp01(r * 8f);
                rings[i].Set(Mathf.Max(0f, r), 0.04f, A(fire, a * f));
            }
            return true;
        });
    }

    // ================= 3.2 普通球膨胀、消失 =================
    // B 是第一轮的"从中心空掉"。用户：方向对，但最后不要剩一道很实的线。第二轮三版都在 B 上改：
    //   D 空掉的同时变淡：空洞往外长，剩下的那圈边越薄越淡，最后是淡没的，不是"一道线啪地没了"。
    //   E 空到一半就散：空洞只长到一半，剩下的宽环整个淡掉，从头到尾没有细线。
    //   F 只淡出：不空，整片往外松一点、淡掉，什么线都不留。

    /// <summary>球被点着的那一刻调</summary>
    public static void Burn(Ball ball)
    {
        if (!Look.On || Look.PopFx == FxV.A || ball.IsGoldBall) return;
        var sr = ball.Renderer;
        Vector2 pos = ball.transform.position;
        Color burn = sr.color;                       // Look.Burn 给的颜色（带透明度）
        var mode = Look.PopFx;

        RingLine hole = null;
        float endAt = -1f;
        FxRun.Go(Track(new GameObject("Pop")), age =>
        {
            if (ball == null) { if (hole != null) Object.Destroy(hole.gameObject); return false; }
            if (ball.CurrentState != Ball.BallState.Shrinking) return true;
            if (hole == null)
            {
                hole = Ring("PopHole", pos, sr.sortingOrder);
                sr.enabled = false;
                endAt = age;
            }
            float t = Mathf.Clamp01((age - endAt) / Look.VanishTime);
            float R = ball.Scale * 0.5f;
            switch (mode)
            {
                case FxV.B:
                {
                    // 中心先空，空洞往外长，最后只剩一圈边，然后没了
                    float w = R * (1f - InOut(t));
                    hole.Set(R - w * 0.5f, w, burn);
                    break;
                }
                case FxV.D:
                {
                    float w = R * (1f - InOut(t));
                    hole.Set(R - w * 0.5f, w, A(burn, burn.a * (1f - t) * (1f - t)));
                    break;
                }
                case FxV.E:
                {
                    float w = R * (1f - 0.55f * OutCubic(t));
                    hole.Set(R - w * 0.5f, w, A(burn, burn.a * (1f - t * t)));
                    break;
                }
                default:
                {
                    float R2 = R * (1f + 0.07f * OutCubic(t));
                    hole.Set(R2 * 0.5f, R2, A(burn, burn.a * (1f - t) * (1f - t)));
                    break;
                }
            }
            return true;
        });
    }

    // ================= 3.4 硬球掉一层壳 =================
    // 第二轮：用户要"炸出碎石、带点小碎片飞溅"。壳本身由 HardShell 画，这里只管碎的那一下。
    //   D 碎石四溅：壳沿着圆周裂成几块，朝四面弹开、打着转、越飞越小；夹着几粒更快的小碎屑。
    //   E 碎石往下掉：弹出来以后带一点重量，划一道弧掉下去。
    //   F 顺着火飞：碎石被火"崩"出去，都往背着火的那一边飞。
    // A（默认）是原来那圈细线散开。

    static readonly System.Random debrisRng = new System.Random(20261118);
    static float Rnd(float a, float b) => a + (float)debrisRng.NextDouble() * (b - a);

    /// <summary>一段壳（从 startDeg 起 sweepDeg 度、内外半径 inner / outer）碎掉。fire 是打它的火相对球心的方向</summary>
    public static void ShellDebris(Vector2 center, float startDeg, float sweepDeg, float inner, float outer, Color color, Vector2 fire)
    {
        var mode = Look.CrackFx;
        if (mode == FxV.A)
        {
            // 原来的样子：一圈（或一段）细线往外散开、变淡
            var arc = Ring("CrackRing", center, 3);
            float mid = (inner + outer) * 0.5f;
            FxRun.Go(arc.gameObject, age =>
            {
                float t = age / 0.45f;
                arc.Set(mid * Mathf.Lerp(1f, 2.1f, OutCubic(t)), 0.05f, A(color, 1f - t), startDeg, sweepDeg);
                return t < 1f;
            });
            return;
        }

        Vector2 away = fire.sqrMagnitude > 0.0001f ? -fire.normalized : Vector2.up;
        float thick = outer - inner, midR = (inner + outer) * 0.5f;
        float arcLen = Mathf.Abs(sweepDeg) * Mathf.Deg2Rad * midR;
        int chunks = Mathf.Clamp(Mathf.RoundToInt(arcLen / (thick * 2.4f)), 3, 8);
        for (int i = 0; i < chunks; i++)
        {
            float deg = startDeg + sweepDeg * (i + Rnd(0.3f, 0.7f)) / chunks;
            var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
            Shard(center + dir * midR, dir, away, thick * Rnd(1.5f, 2.3f), color, mode, false);
        }
        int bits = chunks + 3;
        for (int i = 0; i < bits; i++)
        {
            float deg = startDeg + sweepDeg * Rnd(0f, 1f);
            var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
            Shard(center + dir * outer, dir, away, thick * Rnd(0.5f, 0.85f), color, mode, true);
        }
    }

    static void Shard(Vector2 pos, Vector2 radial, Vector2 away, float size, Color color, FxV mode, bool bit)
    {
        // 一块不规则的四边形或三角形：棱角分明，不是圆的
        int n = bit ? 3 : (debrisRng.Next(2) == 0 ? 4 : 5);
        var pts = new Vector2[n];
        float a0 = Rnd(0f, 360f);
        for (int i = 0; i < n; i++)
        {
            float a = (a0 + 360f * i / n + Rnd(-18f, 18f)) * Mathf.Deg2Rad;
            pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size * 0.5f * Rnd(0.7f, 1.15f);
        }
        var mr = Shapes.Poly(null, bit ? "ShellBit" : "ShellShard", pts, color, 4);
        Track(mr.gameObject);
        mr.transform.position = pos;

        Vector2 vel;
        float life, drag, gravity = 0f;
        float speed = bit ? Rnd(3.4f, 5.6f) : Rnd(1.9f, 3.4f);
        var tangent = new Vector2(-radial.y, radial.x) * Rnd(-0.6f, 0.6f);
        switch (mode)
        {
            case FxV.E:
                vel = (radial + tangent * 0.5f) * speed * 0.8f + Vector2.up * Rnd(0.4f, 1.3f);
                life = Rnd(0.7f, 0.95f); drag = 1.0f; gravity = 10f;
                break;
            case FxV.F:
                vel = (radial * 0.45f + away * 1.25f + tangent * 0.4f) * speed;
                life = Rnd(0.55f, 0.8f); drag = 3.4f;
                break;
            default:
                vel = (radial + tangent) * speed;
                life = Rnd(0.55f, 0.8f); drag = 3.4f;
                break;
        }
        if (bit) life *= 0.75f;
        float spin = Rnd(-520f, 520f);
        Vector2 p = pos;
        float last = 0f;
        FxRun.Go(mr.gameObject, age =>
        {
            float dt = age - last; last = age;
            vel.y -= gravity * dt;
            vel *= Mathf.Exp(-drag * dt);
            p += vel * dt;
            float t = age / life;
            mr.transform.position = p;
            mr.transform.rotation = Quaternion.Euler(0f, 0f, spin * age);
            // 前七成是实实在在的一块，最后三成缩小、淡掉
            float k = Mathf.Clamp01((1f - t) / 0.3f);
            mr.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, k);
            Shapes.SetColor(mr, A(color, k));
            return t < 1f;
        });
    }

    // ================= 3.7 开场 =================

    public static void Intro(List<Ball> balls)
    {
        if (!Look.On || Look.IntroFx == FxV.A || balls.Count == 0) return;
        const float Span = 0.55f, Grow = 0.28f;
        float maxDist = 0.01f;
        foreach (var b in balls) maxDist = Mathf.Max(maxDist, ((Vector2)b.transform.position).magnitude);

        for (int i = 0; i < balls.Count; i++)
        {
            var ball = balls[i];
            var sr = ball.Renderer;
            // B：按建关的顺序一颗接一颗（队伍就是从头到尾）；C：离屏幕中心越远越晚
            float delay = Look.IntroFx == FxV.B
                ? Span * i / Mathf.Max(1, balls.Count - 1)
                : Span * ((Vector2)ball.transform.position).magnitude / maxDist;
            // 球自己的贴图先关掉，用一个子物体从小长到大；球的缩放和判定从头到尾没动过
            var go = new GameObject("IntroProxy");
            go.transform.SetParent(ball.transform, false);
            go.transform.localScale = Vector3.zero;
            var proxy = go.AddComponent<SpriteRenderer>();
            proxy.sprite = sr.sprite;
            proxy.color = sr.color;
            proxy.sortingOrder = sr.sortingOrder;
            sr.enabled = false;
            FxRun.Go(go, age =>
            {
                float t = (age - delay) / Grow;
                // 还没长完就被点着了：立刻交还给球自己（被 Burn 关掉显示的不要再打开）
                if (ball.CurrentState != Ball.BallState.Moving) { sr.enabled = true; return false; }
                if (t >= 1f) { sr.enabled = true; return false; }
                proxy.transform.localScale = Vector3.one * (t <= 0f ? 0f : OutBack(t));
                return true;
            });
        }

        if (Look.IntroFx == FxV.C)
        {
            var sweep = Ring("IntroSweep", Vector2.zero, -1);
            Color ink = Look.Ink;
            FxRun.Go(sweep.gameObject, age =>
            {
                float t = age / (Span + Grow);
                sweep.Set((maxDist + 0.6f) * t, 0.045f, A(ink, 0.32f * (1f - t * t)));
                return t < 1f;
            });
        }
    }

    // ================= 3.7 过关、失败 =================

    public static void Result(bool passed, List<Ball> balls)
    {
        if (!Look.On) return;
        if (passed)
        {
            // 过关的三版都是让满屏的余韵环动一下。如果 3.2 没选余韵（场上没有环），就在烧过的位置现补一圈
            if (Look.AfterglowEnabled && Afterglow.Count == 0)
                foreach (var (pos, size) in burned) Afterglow.SpawnResidue(pos, size * 1.2f, Look.Fire, Look.Ink);
            Vector2 last = burned.Count > 0 ? burned[burned.Count - 1].pos : Vector2.zero;
            Afterglow.Celebrate(Look.WinFx, last);
            // B：一道大环从最后一颗球那里扫出去，扫到哪，哪里的环亮一下就被带走。
            // 第二轮（用户：别用这么实的线，红圈饱和度调低）在 B 上改了三版：
            //   D 细而淡：环细一半、颜色往底色里兑掉一大半。
            //   E 不画大环：只有烧过的位置按远近依次亮一下，"扫过去"是看出来的，不是画出来的。
            //   F 光带：一道很宽、很淡的带子扫过去，没有边线。
            var mode = Look.WinFx;
            if (mode == FxV.B || mode == FxV.D || mode == FxV.F)
            {
                var big = Ring("WinSweep", last, -1);
                Color fire = mode == FxV.B ? Look.Fire : Color.Lerp(Look.Fire, Look.Backdrop, 0.45f);
                float width = mode == FxV.B ? 0.07f : mode == FxV.D ? 0.035f : 0.9f;
                float alpha = mode == FxV.B ? 0.75f : mode == FxV.D ? 0.5f : 0.16f;
                FxRun.Go(big.gameObject, age =>
                {
                    float r = Afterglow.SweepSpeed * age;
                    big.Set(Mathf.Max(width * 0.5f, r), width, A(fire, alpha * Mathf.Clamp01(1f - r / 20f)));
                    return r < 20f;
                });
            }
            return;
        }

        Afterglow.Dismiss();
        if (Look.FailFx == FxV.A) return;
        foreach (var ball in balls)
        {
            if (ball == null || !ball.CountsForTarget) continue;
            if (ball.CurrentState != Ball.BallState.Moving && ball.CurrentState != Ball.BallState.Fleeing) continue;
            var b = ball;
            {
                // 没收到的球各被一圈细环圈出来：差的就是这几颗
                var ring = RingLine.Create(new GameObject("FailMark"), b.transform.position, 3);
                ending.Add(ring.gameObject);
                Color ink = Look.Ink;
                FxRun.Go(ring.gameObject, age =>
                {
                    if (b == null) return false;
                    ring.transform.position = b.transform.position;
                    float r = b.Scale * 0.5f;
                    float t = age / 0.35f;
                    float pulse = 1f + 0.06f * Mathf.Sin(age * 4f);
                    ring.Set(r * Mathf.Lerp(2.6f, 1.6f, OutBack(t)) * (t >= 1f ? pulse : 1f), 0.045f, A(ink, 0.85f * Mathf.Clamp01(t * 2f)));
                    return true;
                });
            }
        }
    }
}

/// <summary>一个每帧调一次的小效果。tick 的参数是出生以来的秒数，返回 false 就把自己连同物体销毁</summary>
public class FxRun : MonoBehaviour
{
    System.Func<float, bool> tick;
    float age;

    public static FxRun Go(GameObject go, System.Func<float, bool> tick)
    {
        var r = go.AddComponent<FxRun>();
        r.tick = tick;
        tick(0f);
        return r;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (tick != null && !tick(age)) Destroy(gameObject);
    }
}

/// <summary>
/// 用线画的圆环 / 圆弧。和贴图环不一样的地方：线宽是世界单位，环放大缩小时粗细不变；
/// 线宽可以一直加到半径那么粗（就成了实心盘），所以也能画"从中心空掉"的盘。
/// </summary>
public class RingLine : MonoBehaviour
{
    const int N = 72;
    static Material mat;
    static readonly Vector3[] buf = new Vector3[N + 1];
    LineRenderer lr;

    public static Material Mat
    {
        get
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            return mat;
        }
    }

    public static RingLine Create(GameObject go, Vector2 pos, int order)
    {
        go.transform.position = pos;
        var r = go.AddComponent<RingLine>();
        r.lr = go.AddComponent<LineRenderer>();
        r.lr.sharedMaterial = Mat;
        r.lr.useWorldSpace = false;
        r.lr.sortingOrder = order;
        r.lr.numCornerVertices = 0;
        r.lr.positionCount = 0;
        return r;
    }

    /// <summary>radius 是线中心的半径；sweepDeg 为负是顺时针</summary>
    public void Set(float radius, float width, Color c, float startDeg = 0f, float sweepDeg = 360f, bool roundCaps = false)
    {
        if (c.a <= 0.001f || width <= 0.0005f || Mathf.Abs(sweepDeg) < 0.5f) { lr.positionCount = 0; return; }
        bool full = Mathf.Abs(sweepDeg) >= 359.9f;
        int n = full ? N : Mathf.Max(2, Mathf.CeilToInt(N * Mathf.Abs(sweepDeg) / 360f) + 1);
        for (int i = 0; i < n; i++)
        {
            float a = (startDeg + sweepDeg * (full ? i / (float)N : i / (float)(n - 1))) * Mathf.Deg2Rad;
            buf[i] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }
        lr.loop = full;
        lr.positionCount = n;
        lr.SetPositions(buf);
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = c;
        lr.numCapVertices = roundCaps && !full ? 5 : 0;
    }
}
