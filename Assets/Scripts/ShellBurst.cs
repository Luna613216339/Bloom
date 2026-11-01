using UnityEngine;

/// <summary>
/// 硬球的壳被打掉时的反馈：一圈细线从球的边上往外散开、变淡。只是画面，不传火。
/// 要让玩家看清"打掉了一层，里面还剩一颗"。
/// </summary>
public class ShellBurst : MonoBehaviour
{
    const float Duration = 0.45f;
    const float EndFactor = 2.1f;   // 散到球直径的几倍

    private SpriteRenderer sr;
    private float startScale;
    private float timer;
    private Color tint;

    public static void Create(Vector2 position, float ballScale, Color tint)
    {
        var go = new GameObject("ShellBurst");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * ballScale;

        var burst = go.AddComponent<ShellBurst>();
        burst.startScale = ballScale;
        burst.tint = tint;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Ring;
        sr.color = tint;
        sr.sortingOrder = 3;
        burst.sr = sr;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Duration);
        float ease = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.one * startScale * Mathf.Lerp(1f, EndFactor, ease);
        sr.color = new Color(tint.r, tint.g, tint.b, tint.a * (1f - t));
        if (t >= 1f) Destroy(gameObject);
    }
}
