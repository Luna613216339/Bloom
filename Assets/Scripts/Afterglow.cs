using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 余韵：一颗球烧完以后，原地留下一圈很淡的细环。纯外观，不碰任何判定。
///
/// 一生分两段：
///   1. 消散 —— 球的实心部分淡掉，同时一圈细环从球边往外扩一点、变淡（「内部消失只剩一圈细环」）。
///   2. 残留 —— 细环不完全消失，留一道很淡的印子，一直到这一关结束。
/// 一关烧完以后，满屏的残留环叠在一起，就是这一关的形状用圆环重新描了一遍。
/// 过关时它们按离中心的远近依次亮一下再散掉（Celebrate）——全游戏的签名时刻，名字叫 Bloom 就是这一下。
/// 没过关时安静地淡掉（Dismiss）。
///
/// 只在正式关卡里用（Look.Afterglow），无尽模式照旧。
/// </summary>
public class Afterglow : MonoBehaviour
{
    static readonly List<Afterglow> live = new List<Afterglow>();

    // 消散段
    const float SpreadTime = 0.55f;      // 细环外扩用的时间
    const float SpreadGrow = 0.22f;      // 外扩到原直径的 1 + 这么多
    const float RingPeakAlpha = 0.55f;   // 细环刚出现时的不透明度
    const float ResidueAlpha = 0.24f;    // 残留印子的不透明度

    // 过关段
    const float WaveSpeed = 6f;          // 亮起的波从中心往外走的速度（世界单位/秒）
    const float FlashTime = 0.6f;       // 每一圈亮起再回落
    const float FlashAlpha = 0.7f;
    const float FlashGrow = 0.18f;
    const float FadeTime = 1.6f;         // 亮完以后散掉

    SpriteRenderer ring;
    float baseSize;
    Color tint;
    Color tintFrom, tintTo;
    float age;

    enum Phase { Spread, Residue, Flash, Fade }
    Phase phase;
    float phaseTime;
    float flashDelay;
    float fadeFrom;
    float sizeFrom;

    public static void Spawn(Vector2 position, float diameter, Color burn, Color residue)
    {
        if (!Look.Afterglow || diameter <= 0.01f) return;

        var go = new GameObject("Afterglow");
        go.transform.position = new Vector3(position.x, position.y, 0.5f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.ThinRing;
        sr.sortingOrder = -1;   // 在球和墙下面

        var a = go.AddComponent<Afterglow>();
        a.ring = sr;
        a.baseSize = diameter;
        a.tintFrom = new Color(burn.r, burn.g, burn.b, 1f);
        a.tintTo = new Color(residue.r, residue.g, residue.b, 1f);
        a.tint = a.tintFrom;
        a.phase = Phase.Spread;
        a.Apply(diameter, RingPeakAlpha);
        live.Add(a);
    }

    /// <summary>过关：从全场残留环的中心往外，一圈一圈依次亮一下，然后一起散掉</summary>
    public static void Celebrate()
    {
        if (live.Count == 0) return;
        Vector2 c = Vector2.zero;
        foreach (var a in live) c += (Vector2)a.transform.position;
        c /= live.Count;
        foreach (var a in live)
        {
            float d = Vector2.Distance(c, a.transform.position);
            a.flashDelay = d / WaveSpeed;
            a.sizeFrom = a.CurrentSize;
            a.fadeFrom = a.ring.color.a;
            a.phase = Phase.Flash;
            a.phaseTime = 0f;
        }
    }

    /// <summary>没过关：残留环安静地淡掉</summary>
    public static void Dismiss()
    {
        foreach (var a in live)
        {
            a.fadeFrom = a.ring.color.a;
            a.sizeFrom = a.CurrentSize;
            a.phase = Phase.Fade;
            a.phaseTime = 0f;
        }
    }

    float CurrentSize => transform.localScale.x;

    void Apply(float size, float alpha)
    {
        transform.localScale = Vector3.one * size;
        ring.color = new Color(tint.r, tint.g, tint.b, alpha);
    }

    void OnDestroy() => live.Remove(this);

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        phaseTime += dt;

        switch (phase)
        {
            case Phase.Spread:
            {
                float t = Mathf.Clamp01(phaseTime / SpreadTime);
                float e = 1f - (1f - t) * (1f - t) * (1f - t);   // 先快后慢
                tint = Color.Lerp(tintFrom, tintTo, e);            // 火的颜色褪成球原来的颜色
                Apply(baseSize * (1f + SpreadGrow * e), Mathf.Lerp(RingPeakAlpha, ResidueAlpha, e));
                if (t >= 1f) { phase = Phase.Residue; phaseTime = 0f; }
                break;
            }
            case Phase.Residue:
                break;

            case Phase.Flash:
            {
                float t = (phaseTime - flashDelay) / FlashTime;
                if (t < 0f) break;
                if (t >= 1f)
                {
                    sizeFrom = CurrentSize;
                    fadeFrom = ring.color.a;
                    phase = Phase.Fade;
                    phaseTime = 0f;
                    break;
                }
                float bump = Mathf.Sin(t * Mathf.PI);                  // 亮起再回落
                float grow = 1f - (1f - t) * (1f - t);                  // 一直往外长一点
                tint = Color.Lerp(tintTo, tintFrom, bump);             // 亮起时短暂回到火的颜色
                Apply(sizeFrom * (1f + FlashGrow * grow), Mathf.Lerp(fadeFrom, FlashAlpha, bump));
                break;
            }
            case Phase.Fade:
            {
                float t = Mathf.Clamp01(phaseTime / FadeTime);
                Apply(sizeFrom * (1f + 0.08f * t), fadeFrom * (1f - t) * (1f - t));
                if (t >= 1f) Destroy(gameObject);
                break;
            }
        }
    }
}
