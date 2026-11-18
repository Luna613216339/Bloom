using UnityEngine;

public class ClickReaction : MonoBehaviour
{
    private float currentScale;
    private float maxScale;
    private float timer;
    private bool shrinking;
    private int sourceId;
    private float vanish = -1f;

    /// <summary>给外观层读：现在的直径、收尾时从 1 淡到 0 的进度</summary>
    public float Scale => currentScale;
    public float Fade01 => vanish >= 0f ? Mathf.Clamp01(vanish / Look.VanishTime) : 1f;

    private const float ExpandSpeed = 5f;
    private const float ShrinkSpeed = 4f;

    public static ClickReaction Create(Vector2 position, float maxScale, float duration)
    {
        var go = new GameObject("ClickReaction");
        go.transform.position = position;
        go.transform.localScale = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Circle;
        bool dark = GameManager.Instance != null && GameManager.Instance.IsDarkBackground;
        sr.color = Look.ClickRing(dark);
        sr.sortingOrder = 2;

        var cr = go.AddComponent<ClickReaction>();
        cr.currentScale = 0f;
        cr.maxScale = maxScale;
        cr.timer = duration;
        cr.shrinking = false;
        cr.sourceId = GameManager.Instance.NextSourceId();

        GameManager.Instance.ReactionStarted();
        Fx.Click(cr, sr);
        return cr;
    }

    void Update()
    {
        if (!shrinking)
        {
            if (currentScale < maxScale)
            {
                currentScale = Mathf.MoveTowards(currentScale, maxScale, ExpandSpeed * Time.deltaTime);
                transform.localScale = Vector3.one * currentScale;
            }

            DetectNearbyBalls();

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                shrinking = true;
                if (Look.SoftVanish)
                {
                    // 玩家点出来的圈也留一道余韵，和球一样：实心淡掉，细环外扩
                    if (Look.Afterglow) Afterglow.Spawn(transform.position, currentScale, Look.Fire, Look.Fire);
                    vanish = Look.VanishTime;
                }
                GameManager.Instance.ReactionEnded();
            }
        }
        else if (vanish >= 0f)
        {
            vanish -= Time.deltaTime;
            var sr = GetComponent<SpriteRenderer>();
            var c = sr.color;
            float k = Mathf.Clamp01(vanish / Look.VanishTime);
            sr.color = new Color(c.r, c.g, c.b, Mathf.Min(c.a, 0.38f * k * k));
            if (vanish <= 0f) Destroy(gameObject);
        }
        else
        {
            currentScale -= ShrinkSpeed * Time.deltaTime;
            if (currentScale <= 0f)
                Destroy(gameObject);
            else
                transform.localScale = Vector3.one * currentScale;
        }
    }

    void DetectNearbyBalls()
    {
        float myRadius = currentScale * 0.5f;
        var balls = GameManager.Instance.AllBalls;
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            var ball = balls[i];
            if (ball.CurrentState != Ball.BallState.Moving && ball.CurrentState != Ball.BallState.Fleeing) continue;
            float ballRadius = ball.transform.localScale.x * 0.5f;
            float dist = Vector2.Distance(transform.position, ball.transform.position);
            if (dist < myRadius + ballRadius)
                ball.Hit(sourceId, transform.position);
        }
    }
}
