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
    Color tintFrom, tintTo, flashTint;
    float age;

    enum Phase { Spread, Residue, Flash, Fade }
    Phase phase;
    float phaseTime;
    float flashDelay;
    float flashTime = FlashTime, flashAlpha = FlashAlpha, flashGrow = FlashGrow, fadeTime = FadeTime;
    float fadeFrom;
    float sizeFrom;

    public static int Count => live.Count;
    public const float SweepSpeed = 11f;   // 过关 B 版那道大环扫出去的速度

    /// <summary>换关 / 重开：上一局的环全部清掉</summary>
    public static void ClearAll()
    {
        foreach (var a in live.ToArray()) if (a != null) Destroy(a.gameObject);
        live.Clear();
    }

    /// <summary>直接生一圈看不见的残留环（场上没有余韵环、过关时又要让烧过的位置亮一下时用）</summary>
    public static void SpawnResidue(Vector2 position, float diameter, Color burn, Color residue)
    {
        var a = Make(position, diameter, burn, residue);
        a.tint = a.tintTo;
        a.phase = Phase.Residue;
        a.Apply(diameter, 0f);
    }

    public static void Spawn(Vector2 position, float diameter, Color burn, Color residue)
    {
        if (!Look.Afterglow || diameter <= 0.01f) return;
        var a = Make(position, diameter, burn, residue);
        a.phase = Phase.Spread;
        a.Apply(diameter, RingPeakAlpha);
    }

    static Afterglow Make(Vector2 position, float diameter, Color burn, Color residue)
    {

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
        live.Add(a);
        return a;
    }

    /// <summary>过关：从全场残留环的中心往外，一圈一圈依次亮一下，然后一起散掉</summary>
    public static void Celebrate() => Celebrate(FxV.A, Vector2.zero);

    /// <summary>
    /// A 从全场的中心往外依次亮；其余几版（B / D / E / F）都是从最后一颗球（origin）扫出去，亮一下马上被带走，
    /// 区别在亮得多红、多重，以及 Fx.Result 里那道大环怎么画。
    /// </summary>
    public static void Celebrate(FxV mode, Vector2 origin)
    {
        if (live.Count == 0) return;
        Vector2 c = Vector2.zero;
        foreach (var a in live) c += (Vector2)a.transform.position;
        c /= live.Count;
        bool sweep = mode != FxV.A;
        foreach (var a in live)
        {
            float d = Vector2.Distance(sweep ? origin : c, a.transform.position);
            a.flashTime = FlashTime; a.flashAlpha = FlashAlpha; a.flashGrow = FlashGrow; a.fadeTime = FadeTime;
            a.flashDelay = d / WaveSpeed;
            a.flashTint = a.tintFrom;
            if (sweep)
            {
                a.flashDelay = d / SweepSpeed; a.flashTime = 0.22f; a.flashAlpha = 0.85f; a.flashGrow = 0.1f; a.fadeTime = 0.35f;
                if (mode != FxV.B)
                {
                    // 第二轮的三版：亮起来的颜色往底色里兑一些，不那么红；亮得也轻一点、慢一点
                    // 而且不是一圈线，是一片很淡的实心圆：像这一关又轻轻开了一次（用户：不要实心的线条）
                    a.ring.sprite = SpriteHelper.Circle;
                    a.flashTint = Color.Lerp(a.tintFrom, Look.Backdrop, 0.25f);
                    a.tintTo = a.flashTint;      // 从头到尾都是这个淡红，不经过球的墨色（否则一片灰）
                    a.flashAlpha = mode == FxV.E ? 0.3f : 0.2f;
                    a.flashTime = mode == FxV.E ? 0.42f : 0.3f;
                    a.flashGrow = 0.16f; a.fadeTime = 0.5f;
                }
            }
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
            a.fadeTime = FadeTime;
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
                float t = (phaseTime - flashDelay) / flashTime;
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
                tint = Color.Lerp(tintTo, flashTint, bump);            // 亮起时短暂回到火的颜色
                Apply(sizeFrom * (1f + flashGrow * grow), Mathf.Lerp(fadeFrom, flashAlpha, bump));
                break;
            }
            case Phase.Fade:
            {
                float t = Mathf.Clamp01(phaseTime / fadeTime);
                Apply(sizeFrom * (1f + 0.08f * t), fadeFrom * (1f - t) * (1f - t));
                if (t >= 1f) Destroy(gameObject);
                break;
            }
        }
    }
}
