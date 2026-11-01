using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 让一团静止的球"活着"：按 IdleDef 的动法小幅移动。只动还没被点着的球（Moving 状态），
/// 点着的那颗立刻停在原地。球是 Kinematic 的，用 MovePosition 推，连锁判定读的 transform 跟着走。
///
/// 五种动法：
///   Bob     整团上下浮
///   Sway    整团左右摆
///   Spin    整团绕自己的重心慢转（连续转，不是摆）
///   Breathe 整团微微胀缩（球距会跟着变，幅度压得很小）
///   Drift   每颗各自轻微漂移（李萨如曲线，相位和频率各不相同）
/// </summary>
public class IdleGroup : MonoBehaviour
{
    struct Entry
    {
        public Ball ball;
        public Rigidbody2D rb;
        public Vector2 basePos;
        public float phaseA, phaseB, wA, wB;
    }

    readonly List<Entry> entries = new List<Entry>();
    IdleDef def;
    Vector2 centroid;
    float maxRadius;
    float t;

    public void Configure(IdleDef def, List<Ball> balls)
    {
        this.def = def;
        var rng = new System.Random(def.Seed);
        centroid = Vector2.zero;
        foreach (var b in balls) centroid += (Vector2)b.transform.position;
        if (balls.Count > 0) centroid /= balls.Count;
        maxRadius = 0.5f;
        foreach (var b in balls)
        {
            var p = (Vector2)b.transform.position;
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(p, centroid));
            entries.Add(new Entry
            {
                ball = b,
                rb = b.GetComponent<Rigidbody2D>(),
                basePos = p,
                phaseA = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                phaseB = (float)(rng.NextDouble() * Mathf.PI * 2.0),
                wA = Mathf.Lerp(0.7f, 1.2f, (float)rng.NextDouble()),
                wB = Mathf.Lerp(0.7f, 1.2f, (float)rng.NextDouble()),
            });
        }
    }

    void FixedUpdate()
    {
        if (def == null || def.Mode == IdleMode.None) return;
        t += Time.fixedDeltaTime;
        float w = Mathf.PI * 2f / Mathf.Max(0.1f, def.Period);

        foreach (var e in entries)
        {
            if (e.ball == null || e.ball.CurrentState != Ball.BallState.Moving) continue;
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
                    Vector2 r = e.basePos - centroid;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    p = centroid + new Vector2(r.x * c - r.y * s, r.x * s + r.y * c);
                    break;
                }
                case IdleMode.Breathe:
                {
                    float k = def.Amplitude / maxRadius * Mathf.Sin(w * t);
                    p = e.basePos + (e.basePos - centroid) * k;
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
