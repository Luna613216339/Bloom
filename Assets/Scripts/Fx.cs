using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画方案（2026-11-18 美术方案审核）：每一项 A / B / C 三版里的 B 和 C 都在这个文件，A 是原来的代码。
/// 选哪一版看 Look.cs 里的 ClickFx / PopFx / FleeFx / CrackFx / SparkFx / DoorFx / IntroFx / WinFx / FailFx。
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

    public static void Click(ClickReaction cr, SpriteRenderer sr)
    {
        if (!Look.On || Look.ClickFx == FxV.A) return;
        Vector2 pos = cr.transform.position;
        Color fire = Look.Fire;
        sr.enabled = false;

        if (Look.ClickFx == FxV.B)
        {
            // 细环加落点：只画判定的那一圈边，里面几乎是空的，点下去的地方留一颗小点
            var ring = Ring("ClickRing", pos, 2);
            var fill = Disc("ClickFill", pos, 0f, A(fire, 0.07f), 0);
            var dot = Disc("ClickDot", pos, 0.17f, fire, 2);
            const float w = 0.07f;
            FxRun.Go(ring.gameObject, age =>
            {
                if (cr == null)
                {
                    Object.Destroy(fill.gameObject); Object.Destroy(dot.gameObject);
                    return false;
                }
                float r = cr.Scale * 0.5f, f = cr.Fade01;
                ring.Set(Mathf.Max(0f, r - w * 0.5f), Mathf.Min(w, r), A(fire, 0.95f * f));
                fill.transform.localScale = Vector3.one * cr.Scale;
                fill.color = A(fire, 0.07f * f);
                dot.transform.localScale = Vector3.one * 0.17f * Mathf.Min(1f, age / 0.08f) * f;
                return true;
            });
        }
        else
        {
            // 三道涟漪：最外一圈是判定边界，里面两圈慢一拍跟出来，先后撞到边上
            var outer = Ring("ClickRing", pos, 2);
            var in1 = Ring("ClickRipple1", pos, 2);
            var in2 = Ring("ClickRipple2", pos, 2);
            var fill = Disc("ClickFill", pos, 0f, A(fire, 0.16f), 0);
            const float w = 0.06f;
            FxRun.Go(outer.gameObject, age =>
            {
                if (cr == null)
                {
                    Object.Destroy(in1.gameObject); Object.Destroy(in2.gameObject); Object.Destroy(fill.gameObject);
                    return false;
                }
                float r = cr.Scale * 0.5f, f = cr.Fade01;
                outer.Set(Mathf.Max(0f, r - w * 0.5f), Mathf.Min(w, r), A(fire, 0.95f * f));
                fill.transform.localScale = Vector3.one * cr.Scale;
                fill.color = A(fire, 0.16f * f);
                float r1 = 1.5f * (age - 0.10f), r2 = 0.95f * (age - 0.20f);
                in1.Set(Mathf.Max(0f, r1), 0.045f, A(fire, r1 > 0f && r1 < r - 0.03f ? 0.6f * f : 0f));
                in2.Set(Mathf.Max(0f, r2), 0.04f, A(fire, r2 > 0f && r2 < r - 0.03f ? 0.42f * f : 0f));
                return true;
            });
        }
    }

    // ================= 3.2 普通球膨胀、消失 =================

    /// <summary>球被点着的那一刻调。B：实心盘照旧，烧完从中心空掉；C：空心环加一颗实心小核</summary>
    public static void Burn(Ball ball)
    {
        if (!Look.On || Look.PopFx == FxV.A || ball.IsGoldBall) return;
        var sr = ball.Renderer;
        Vector2 pos = ball.transform.position;
        float baseSize = ball.Scale;
        Color burn = sr.color;                       // Look.Burn 给的颜色（带透明度）
        Color fire = A(burn, 1f);

        if (Look.PopFx == FxV.B)
        {
            RingLine hole = null;
            float endAt = -1f;
            FxRun.Go(Track(new GameObject("PopB")), age =>
            {
                if (ball == null) { if (hole != null) Object.Destroy(hole.gameObject); return false; }
                if (ball.CurrentState != Ball.BallState.Shrinking) return true;
                if (hole == null)
                {
                    hole = Ring("PopHole", pos, sr.sortingOrder);
                    sr.enabled = false;
                    endAt = age;
                }
                // 中心先空，空洞往外长，最后只剩一圈边，然后没了
                float h = InOut((age - endAt) / Look.VanishTime);
                float R = ball.Scale * 0.5f, w = R * (1f - h);
                hole.Set(R - w * 0.5f, w, burn);
                return true;
            });
        }
        else
        {
            sr.enabled = false;
            var core = Disc("PopCore", pos, baseSize, fire, 2);
            var fill = Disc("PopFill", pos, baseSize, A(fire, 0.10f), 0);
            var ring = Ring("PopRing", pos, 1);
            const float w = 0.055f;
            float endAt = -1f;
            FxRun.Go(ring.gameObject, age =>
            {
                if (ball == null)
                {
                    Object.Destroy(core.gameObject); Object.Destroy(fill.gameObject);
                    return false;
                }
                float f = 1f;
                if (ball.CurrentState == Ball.BallState.Shrinking)
                {
                    if (endAt < 0f) endAt = age;
                    f = 1f - Mathf.Clamp01((age - endAt) / Look.VanishTime);
                }
                float R = ball.Scale * 0.5f;
                ring.Set(Mathf.Max(0f, R - w * 0.5f), w, A(fire, 0.9f * f));
                fill.transform.localScale = Vector3.one * ball.Scale;
                fill.color = A(fire, 0.10f * f);
                core.transform.localScale = Vector3.one * baseSize * f;
                return true;
            });
        }
    }

    // ================= 3.3 逃跑球 =================

    public static void Flee(Ball ball)
    {
        var sr = ball.Renderer;
        Color c = A(sr.color, 1f);
        float size = ball.Scale;
        Vector2 last = ball.transform.position;

        if (Look.FleeFx == FxV.B)
        {
            // 先一惊：原地弹出一圈细环；跑的时候身后留一串越来越小的点
            var alert = Ring("FleeAlert", last, 2);
            Vector2 at = last;
            FxRun.Go(alert.gameObject, age =>
            {
                float t = age / 0.38f;
                alert.Set(size * 0.5f * Mathf.Lerp(1.1f, 2.6f, OutCubic(t)), 0.05f, A(c, 0.9f * (1f - t)));
                return t < 1f;
            });
            float travelled = 0f;
            FxRun.Go(Track(new GameObject("FleeDots")), age =>
            {
                if (ball == null || ball.CurrentState != Ball.BallState.Fleeing) return false;
                Vector2 p = ball.transform.position;
                travelled += Vector2.Distance(p, last);
                last = p;
                if (travelled >= 0.3f)
                {
                    travelled = 0f;
                    var dot = Disc("FleeDot", p, size * 0.6f, A(c, 0.8f), 0);
                    FxRun.Go(dot.gameObject, a2 =>
                    {
                        float k = 1f - a2 / 0.55f;
                        dot.transform.localScale = Vector3.one * size * 0.6f * Mathf.Max(0f, k);
                        return k > 0f;
                    });
                }
                return true;
            });
        }
        else
        {
            // 残影：身后跟着三个越来越淡的自己；停下来以后吐出一圈环，像喘了口气
            var ghosts = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++) ghosts[i] = Disc("FleeGhost", last, size, A(c, 0.34f - i * 0.1f), 0);
            var trail = new List<(float t, Vector2 p)>();
            float stoppedAt = -1f;
            bool moved = false;
            FxRun.Go(Track(new GameObject("FleeGhosts")), age =>
            {
                bool gone = ball == null || ball.CurrentState != Ball.BallState.Fleeing;
                if (!gone)
                {
                    Vector2 p = ball.transform.position;
                    trail.Add((age, p));
                    bool moving = ball.Body.linearVelocity.sqrMagnitude > 0.01f;
                    if (moving) moved = true;
                    if (moved && !moving && stoppedAt < 0f)
                    {
                        stoppedAt = age;
                        var puff = Ring("FleePuff", p, 0);
                        FxRun.Go(puff.gameObject, a2 =>
                        {
                            float t = a2 / 0.45f;
                            puff.Set(size * 0.5f * Mathf.Lerp(1.05f, 1.9f, OutCubic(t)), 0.045f, A(c, 0.7f * (1f - t)));
                            return t < 1f;
                        });
                    }
                }
                bool done = gone || (stoppedAt >= 0f && age - stoppedAt > 0.25f);
                for (int i = 0; i < 3; i++)
                {
                    if (done) { Object.Destroy(ghosts[i].gameObject); continue; }
                    float want = age - 0.045f * (i + 1);
                    Vector2 gp = trail[0].p;
                    for (int k = trail.Count - 1; k >= 0; k--) if (trail[k].t <= want) { gp = trail[k].p; break; }
                    ghosts[i].transform.position = gp;
                }
                return !done;
            });
        }
    }

    // ================= 3.4 硬球碎壳 =================

    public static void Crack(Ball ball)
    {
        var sr = ball.Renderer;
        Color c = A(sr.color, 1f);
        float r = ball.Scale * 0.5f;
        Vector2 pos = ball.transform.position;

        if (Look.CrackFx == FxV.B)
        {
            // 壳裂成三段弧飞开；之后那一秒不闪，球外一圈环像倒计时一样走完，走完才吃得进下一把火
            for (int i = 0; i < 3; i++)
            {
                var arc = Ring("CrackArc", pos, 3);
                float start = 20f + 120f * i;
                FxRun.Go(arc.gameObject, age =>
                {
                    float t = age / 0.45f, e = OutCubic(t);
                    arc.Set(r * Mathf.Lerp(1.0f, 2.0f, e), 0.055f, A(c, 1f - t), start + 30f * e, 96f, true);
                    return t < 1f;
                });
            }
            var clock = Ring("CrackClock", pos, 3);
            FxRun.Go(clock.gameObject, age =>
            {
                if (ball == null || ball.CurrentState != Ball.BallState.Moving) return false;
                float t = age / Ball.HardCooldown;
                if (t >= 1f) return false;
                clock.transform.position = ball.transform.position;
                clock.Set(r * 1.55f, 0.05f, A(c, 0.85f), 90f, -360f * (1f - t), true);
                return true;
            });
        }
        else
        {
            // 壳不往外散，往里一收贴到球上；球一下子变浅，再用一秒慢慢回到本色（深浅就是"还要等多久"）
            var shell = Ring("CrackShell", pos, 3);
            FxRun.Go(shell.gameObject, age =>
            {
                float t = age / 0.2f;
                if (ball != null) shell.transform.position = ball.transform.position;
                shell.Set(r * Mathf.Lerp(1.7f, 1.0f, OutCubic(t)), 0.06f, A(c, 0.9f * (1f - t * t)));
                return t < 1f;
            });
            Color light = Color.Lerp(c, Look.Backdrop, 0.68f);
            FxRun.Go(Track(new GameObject("CrackTint")), age =>
            {
                if (ball == null || ball.CurrentState != Ball.BallState.Moving) return false;
                float t = age / Ball.HardCooldown;
                sr.color = Color.Lerp(light, c, Mathf.Clamp01(t * t));
                return t < 1f;
            });
        }
    }

    // ================= 3.5 分裂球、火星 =================

    /// <summary>分裂的那一下。B：母球处"啵"地一圈环；C：朝两个方向各闪一道短线</summary>
    public static void SplitMoment(Vector2 pos, Vector2 a, Vector2 b, float size)
    {
        if (!Look.On || Look.SparkFx == FxV.A) return;
        Color fire = Look.Fire;
        if (Look.SparkFx == FxV.B)
        {
            var ring = Ring("SplitPulse", pos, 3);
            FxRun.Go(ring.gameObject, age =>
            {
                float t = age / 0.3f;
                ring.Set(size * 0.5f * Mathf.Lerp(1f, 2.4f, OutCubic(t)), 0.05f, A(fire, 0.9f * (1f - t)));
                return t < 1f;
            });
        }
        else
        {
            foreach (var d in new[] { a, b })
            {
                Vector2 dir = d;
                var lr = Line("SplitStreak", 3, 0.06f, fire);
                FxRun.Go(lr.gameObject, age =>
                {
                    float t = age / 0.22f;
                    float head = size * 0.5f + Ball.FragmentSpeed * Mathf.Min(age, 0.22f);
                    float tail = Mathf.Lerp(size * 0.5f, head, OutCubic(t));
                    lr.SetPosition(0, pos + dir * tail);
                    lr.SetPosition(1, pos + dir * head);
                    lr.startColor = lr.endColor = A(fire, 0.8f * (1f - t));
                    return t < 1f;
                });
            }
        }
    }

    /// <summary>火星飞行时的样子。B：身后留一串越来越小的点；C：空心小环加两道速度线</summary>
    public static void Spark(Ball f)
    {
        var sr = f.Renderer;
        Color fire = Look.Fire;
        float size = f.Scale;
        Vector2 last = f.transform.position;

        if (Look.SparkFx == FxV.B)
        {
            float travelled = 0.2f;
            FxRun.Go(Track(new GameObject("SparkDots")), age =>
            {
                if (f == null || f.CurrentState != Ball.BallState.Moving) return false;
                Vector2 p = f.transform.position;
                travelled += Vector2.Distance(p, last);
                last = p;
                if (travelled >= 0.27f)
                {
                    travelled = 0f;
                    var dot = Disc("SparkDot", p, size * 0.8f, A(fire, 0.9f), 0);
                    FxRun.Go(dot.gameObject, a2 =>
                    {
                        float k = 1f - a2 / 0.6f;
                        dot.transform.localScale = Vector3.one * size * 0.8f * Mathf.Max(0f, k);
                        return k > 0f;
                    });
                }
                return true;
            });
        }
        else
        {
            // 球自己那张实心贴图关掉，换成一圈线画的环（线宽固定，比贴图环清楚）；到点炸开时 Ball.Pop 会把贴图打开
            sr.enabled = false;
            var hollow = Ring("SparkRing", last, 2);
            var l1 = Line("SparkLine", 0, 0.045f, fire);
            var l2 = Line("SparkLine", 0, 0.045f, fire);
            FxRun.Go(l1.gameObject, age =>
            {
                if (f == null || f.CurrentState != Ball.BallState.Moving)
                {
                    Object.Destroy(l2.gameObject); Object.Destroy(hollow.gameObject);
                    return false;
                }
                hollow.transform.position = f.transform.position;
                hollow.Set(size * 0.5f - 0.025f, 0.05f, fire);
                Vector2 v = f.Body.linearVelocity;
                if (v.sqrMagnitude < 0.01f) return true;
                Vector2 d = v.normalized, n = new Vector2(-d.y, d.x), p = f.transform.position;
                float near = size * 0.5f + 0.09f, len = Mathf.Min(0.42f, age * Ball.FragmentSpeed);
                l1.SetPosition(0, p - d * near + n * 0.075f); l1.SetPosition(1, p - d * (near + len) + n * 0.075f);
                l2.SetPosition(0, p - d * near - n * 0.075f); l2.SetPosition(1, p - d * (near + len) - n * 0.075f);
                return true;
            });
        }
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
                if (ball.CurrentState != Ball.BallState.Moving) { if (Look.PopFx != FxV.C) sr.enabled = true; return false; }
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
            if (Look.WinFx == FxV.B)
            {
                // 一道大环从最后一颗球那里扫出去，扫到哪，哪里的环亮一下就被带走
                var big = Ring("WinSweep", last, -1);
                Color fire = Look.Fire;
                FxRun.Go(big.gameObject, age =>
                {
                    float r = Afterglow.SweepSpeed * age;
                    big.Set(r, 0.07f, A(fire, 0.75f * Mathf.Clamp01(1f - r / 20f)));
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
            if (Look.FailFx == FxV.B)
            {
                // 没收到的球褪成浅色：盖一片底色的半透明圆（跟着球走）
                var go = new GameObject("FailVeil");
                go.transform.SetParent(b.transform, false);
                go.transform.localScale = Vector3.one * 1.06f;
                var veil = go.AddComponent<SpriteRenderer>();
                veil.sprite = SpriteHelper.Circle;
                veil.sortingOrder = b.Renderer.sortingOrder + 1;
                Color bg = Look.Backdrop;
                veil.color = A(bg, 0f);
                ending.Add(go);
                FxRun.Go(go, age => { veil.color = A(bg, 0.62f * OutCubic(age / 0.5f)); return true; });
            }
            else
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
