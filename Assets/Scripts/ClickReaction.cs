using UnityEngine;

public class ClickReaction : MonoBehaviour
{
    private float currentScale;
    private float maxScale;
    private float timer;
    private bool shrinking;

    private const float ExpandSpeed = 5f;
    private const float ShrinkSpeed = 4f;

    public static ClickReaction Create(Vector2 position, float maxScale, float duration)
    {
        var go = new GameObject("ClickReaction");
        go.transform.position = position;
        go.transform.localScale = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Circle;
        sr.color = new Color(0.6f, 0.6f, 0.6f, 0.45f);
        sr.sortingOrder = 2;

        var cr = go.AddComponent<ClickReaction>();
        cr.currentScale = 0f;
        cr.maxScale = maxScale;
        cr.timer = duration;
        cr.shrinking = false;

        GameManager.Instance.ReactionStarted();
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
                GameManager.Instance.ReactionEnded();
            }
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
            if (ball.CurrentState != Ball.BallState.Moving) continue;
            float ballRadius = ball.transform.localScale.x * 0.5f;
            float dist = Vector2.Distance(transform.position, ball.transform.position);
            if (dist < myRadius + ballRadius)
                ball.Trigger();
        }
    }
}
