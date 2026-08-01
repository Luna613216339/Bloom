using UnityEngine;

/// <summary>
/// 金球被连锁烧到时的反馈。不是放大动画 —— 美元符号以最终尺寸直接出现在
/// 膨胀圈里，像一个投影，然后淡出。
///
/// 尺寸、颜色、时长都是下面这几个常量，要调直接改。
/// </summary>
public class GoldBurst : MonoBehaviour
{
    const float HoldTime = 0.18f;   // 完全不透明地停留多久
    const float FadeTime = 0.45f;   // 之后淡出多久
    const float SizeFactor = 1.35f; // 相对金球原始大小

    private SpriteRenderer sr;
    private float timer;
    private Color tint;

    public static void Create(Vector2 position, Color tint, float ballScale)
    {
        var go = new GameObject("GoldBurst");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * ballScale * SizeFactor;

        var burst = go.AddComponent<GoldBurst>();
        burst.tint = tint;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Dollar;
        sr.color = tint;
        sr.sortingOrder = 4;
        burst.sr = sr;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer <= HoldTime) return;

        float t = Mathf.Clamp01((timer - HoldTime) / FadeTime);
        sr.color = new Color(tint.r, tint.g, tint.b, 1f - t);

        if (t >= 1f)
            Destroy(gameObject);
    }
}
