using UnityEngine;

public class Ball : MonoBehaviour
{
    public enum BallState { Moving, Triggered, Shrinking, Fleeing }
    public BallState CurrentState { get; private set; } = BallState.Moving;
    public bool IsEscapeBall { get; private set; }
    public bool IsGoldBall { get; private set; }

    // 金球在静止的球阵里是唯一会"呼吸"的东西，眼睛一定先找到它。
    // 这比任何颜色都管用 —— 平涂的金色在一堆彩球里只是"又一个颜色"。
    const float PulseAmplitude = 0.06f;
    const float PulseSpeed = 3.6f;
    const float GoldSizeBoost = 1.18f;   // 金币比普通球大一圈，否则视觉上反而显小
    const float SpinSpeedMin = 40f;      // 度/秒，让美元符号在飞行中缓慢自转
    const float SpinSpeedMax = 90f;
    static readonly Color GoldColor = new Color(1f, 0.78f, 0.22f);

    private float baseScale;
    private float pulsePhase;
    private float spinSpeed;

    private const int BallLayer = 6;
    private const float ShrinkSpeed = 4f;

    private float expandSpeed = 5f;
    private float dangerRadius = 2f;
    private float fleeSpeed = 6f;
    private Vector2 preferredFleeDir;

    public static PhysicsMaterial2D bounceMat;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private float currentScale;
    private float maxScale;
    private float reactionDuration;
    private float triggerTimer;

    public static void SetupPhysics()
    {
        Physics2D.IgnoreLayerCollision(BallLayer, BallLayer, true);
        Physics2D.bounceThreshold = 0f;
        if (bounceMat == null)
        {
            bounceMat = new PhysicsMaterial2D("BallBounce");
            bounceMat.bounciness = 1f;
            bounceMat.friction = 0f;
        }
    }

    public static Ball Create(Vector2 position, Vector2 velocity, Color color,
        float size, float maxScale, float reactionDuration, float expandSpeed = 5f)
    {
        var go = new GameObject("Ball");
        go.layer = BallLayer;
        go.transform.position = position;
        go.transform.localScale = Vector3.one * size;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteHelper.Circle;
        sr.color = color;
        sr.sortingOrder = 1;

        if (bounceMat == null)
        {
            bounceMat = new PhysicsMaterial2D("BallBounce");
            bounceMat.bounciness = 1f;
            bounceMat.friction = 0f;
        }

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.angularDamping = 0f;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = velocity;

        var col = go.AddComponent<CircleCollider2D>();
        col.sharedMaterial = bounceMat;

        var ball = go.AddComponent<Ball>();
        ball.sr = sr;
        ball.rb = rb;
        ball.col = col;
        ball.currentScale = size;
        ball.maxScale = maxScale;
        ball.reactionDuration = reactionDuration;
        ball.expandSpeed = expandSpeed;

        GameManager.Instance.RegisterBall(ball);
        return ball;
    }

    public void SetAsGoldBall()
    {
        IsGoldBall = true;
        baseScale = currentScale * GoldSizeBoost;
        currentScale = baseScale;
        transform.localScale = Vector3.one * baseScale;
        pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        spinSpeed = Random.Range(SpinSpeedMin, SpinSpeedMax) * (Random.value < 0.5f ? -1f : 1f);
        // 金币的颜色是烤进贴图里的（外环/内盘/符号三段），所以这里不染色
        sr.sprite = SpriteHelper.Coin;
        sr.color = Color.white;
        sr.sortingOrder = 2;
    }

    public void SetAsEscapeBall(float dangerRadius, float fleeSpeed, Color color)
    {
        IsEscapeBall = true;
        this.dangerRadius = dangerRadius;
        this.fleeSpeed = fleeSpeed;
        sr.sprite = SpriteHelper.StripedCircle;
        sr.color = color;
    }

    public void SetPreferredFleeDirection(Vector2 dir)
    {
        preferredFleeDir = dir.normalized;
    }

    void Update()
    {
        if (IsEscapeBall && CurrentState == BallState.Moving)
            CheckForDanger();

        if (IsGoldBall && CurrentState == BallState.Moving)
        {
            pulsePhase += Time.deltaTime * PulseSpeed;
            float s = baseScale * (1f + Mathf.Sin(pulsePhase) * PulseAmplitude);
            currentScale = s;
            transform.localScale = Vector3.one * s;

            // Rigidbody 勾了 FreezeRotation，物理不会转它，所以直接改 transform 就行
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        switch (CurrentState)
        {
            case BallState.Triggered:
                if (currentScale < maxScale)
                {
                    currentScale = Mathf.MoveTowards(currentScale, maxScale, expandSpeed * Time.deltaTime);
                    transform.localScale = Vector3.one * currentScale;
                }
                DetectNearbyBalls();
                triggerTimer -= Time.deltaTime;
                if (triggerTimer <= 0f)
                {
                    CurrentState = BallState.Shrinking;
                    GameManager.Instance.ReactionEnded();
                }
                break;

            case BallState.Shrinking:
                currentScale -= ShrinkSpeed * Time.deltaTime;
                if (currentScale <= 0f)
                {
                    GameManager.Instance.UnregisterBall(this);
                    Destroy(gameObject);
                }
                else
                {
                    transform.localScale = Vector3.one * currentScale;
                }
                break;
        }
    }

    void CheckForDanger()
    {
        var balls = GameManager.Instance.AllBalls;
        Vector2 myPos = transform.position;

        for (int i = 0; i < balls.Count; i++)
        {
            var other = balls[i];
            if (other == this || other.CurrentState != BallState.Triggered) continue;

            float dist = Vector2.Distance(myPos, other.transform.position);
            if (dist < dangerRadius)
            {
                Vector2 fleeDir = preferredFleeDir != Vector2.zero
                    ? preferredFleeDir
                    : (myPos - (Vector2)other.transform.position).normalized;
                StartFleeing(fleeDir);
                return;
            }
        }
    }

    void StartFleeing(Vector2 direction)
    {
        CurrentState = BallState.Fleeing;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = direction * fleeSpeed;
        col.enabled = true;

        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = currentScale * 0.3f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        var c = sr.color;
        trail.startColor = new Color(c.r, c.g, c.b, 0.6f);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
        trail.sortingOrder = 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (CurrentState != BallState.Fleeing) return;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void DetectNearbyBalls()
    {
        float myRadius = currentScale * 0.5f;
        var balls = GameManager.Instance.AllBalls;
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            var other = balls[i];
            if (other == this) continue;
            if (other.CurrentState != BallState.Moving && other.CurrentState != BallState.Fleeing) continue;
            float otherRadius = other.currentScale * 0.5f;
            float dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist < myRadius + otherRadius)
                other.Trigger();
        }
    }

    public void Trigger()
    {
        if (CurrentState != BallState.Moving && CurrentState != BallState.Fleeing) return;
        CurrentState = BallState.Triggered;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        triggerTimer = reactionDuration;

        if (IsGoldBall)
        {
            // 金球膨胀时换回普通圆形，否则高光会被拉得很怪
            sr.sprite = SpriteHelper.Circle;
            GoldBurst.Create(transform.position, GoldColor, currentScale);
        }

        // 半透明在白底上是"变浅"，在深色底上是"变暗" —— 后者会把霓虹球的光感抽掉，
        // 所以深色主题下留多一点不透明度
        var c = sr.color;
        float alpha = GameManager.Instance.IsDarkBackground ? 0.7f : 0.45f;
        sr.color = new Color(c.r, c.g, c.b, alpha);
        GameManager.Instance.OnBallTriggered(this);
        GameManager.Instance.ReactionStarted();
    }
}
