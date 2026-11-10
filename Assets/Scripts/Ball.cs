using UnityEngine;

public class Ball : MonoBehaviour
{
    public enum BallState { Moving, Triggered, Shrinking, Fleeing }
    public BallState CurrentState { get; private set; } = BallState.Moving;
    public BallKind Kind { get; private set; } = BallKind.Normal;
    public bool IsEscapeBall { get; private set; }
    public bool IsGoldBall { get; private set; }

    /// <summary>硬球的壳已经被打掉了（火要再来一次才炸）</summary>
    public bool IsCracked => Kind == BallKind.Hard && crackedAt >= 0f;

    /// <summary>
    /// 硬球打掉壳之后多久内碰到它的火都不算"第二次"。连锁里相邻的球是接连着火的，
    /// 同一波火的几颗邻居会在这段时间里先后碰到它；要等火从别的路绕回来、或者火星飞到，才是第二次。
    /// </summary>
    public const float HardCooldown = 1.0f;

    /// <summary>火星不算通关数，也不算场上球数</summary>
    public bool CountsForTarget => Kind != BallKind.Fragment;

    /// <summary>
    /// 这颗球作为"一把火"的编号。硬球靠它分辨两次碰它的是不是同一把火。
    /// 被触发时才有值；火星一出生就有。
    /// </summary>
    public int SourceId { get; private set; }

    // ---- 分裂球 / 火星的规则参数（样关靠这些数摆位置，改了要同步看 LevelCatalog）----
    /// <summary>火星背着火源飞，左右各偏这个角度</summary>
    public const float SplitHalfAngleDeg = 40f;
    public const float FragmentSpeed = 4.5f;
    /// <summary>火星烧多久。到时间自己"啪"地炸一下小的</summary>
    public const float FragmentFuse = 1.6f;
    const float FragmentSizeRatio = 0.55f;
    const float FragmentPopScale = 0.9f;
    const float FragmentPopDuration = 0.8f;

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

    private float splitAxisDeg = float.NaN;
    private float splitAxisDeg2 = float.NaN;
    private float crackedAt = -1f;
    private readonly System.Collections.Generic.HashSet<int> spentSources = new System.Collections.Generic.HashSet<int>();
    private float fuse;

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
        sr.color = Look.Escape(color);
    }

    public void SetPreferredFleeDirection(Vector2 dir)
    {
        preferredFleeDir = dir.normalized;
    }

    /// <summary>
    /// 分裂球：五彩外观，颜色烤在贴图里，不跟关卡配色走 —— 它必须在任何配色里都一眼认出来。
    /// axisDeg 给了就是定向的：两颗火星沿固定方向飞（axisDeg2 不给就和第一颗正相反），并且画出瞄准虚线；
    /// 不给（NaN）就背着火源飞，没有虚线。
    /// </summary>
    public void SetAsSplitBall(float axisDeg = float.NaN, float axisDeg2 = float.NaN)
    {
        Kind = BallKind.Split;
        splitAxisDeg = axisDeg;
        splitAxisDeg2 = axisDeg2;
        sr.sprite = SpriteHelper.Rainbow;
        sr.color = Color.white;
        sr.sortingOrder = 2;
        if (IsAimed && Look.AimLines) gameObject.AddComponent<AimLine>().Configure(this);
    }

    /// <summary>定向的分裂球（火星方向固定，和火从哪来无关）</summary>
    public bool IsAimed => Kind == BallKind.Split && !float.IsNaN(splitAxisDeg);

    /// <summary>定向分裂球的两个火星方向。瞄准虚线和真正分裂用的是同一份</summary>
    public void AimDirections(out Vector2 a, out Vector2 b)
    {
        float r = splitAxisDeg * Mathf.Deg2Rad;
        a = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        if (float.IsNaN(splitAxisDeg2)) b = -a;
        else
        {
            float r2 = splitAxisDeg2 * Mathf.Deg2Rad;
            b = new Vector2(Mathf.Cos(r2), Mathf.Sin(r2));
        }
    }

    /// <summary>火星从出生到自己炸掉能飞多远</summary>
    public const float FragmentRange = FragmentSpeed * FragmentFuse;
    /// <summary>火星的半径（相对分裂球直径）和出生时离球心的距离，瞄准虚线照这个描</summary>
    public float FragmentRadius => currentScale * FragmentSizeRatio * 0.5f;
    public float FragmentSpawnOffset => currentScale * 0.5f;

    /// <summary>硬球：外面多一层壳。壳用贴图里的透明环表现，颜色仍是关卡配色</summary>
    public void SetAsHardBall()
    {
        Kind = BallKind.Hard;
        sr.sprite = SpriteHelper.Shell;
        sr.color = Look.Hard(sr.color);
    }

    static Ball CreateFragment(Vector2 position, Vector2 dir, float parentSize, float duration)
    {
        float size = parentSize * FragmentSizeRatio;
        var f = Create(position + dir * (parentSize * 0.5f), dir * FragmentSpeed, Color.white,
            size, FragmentPopScale, FragmentPopDuration);
        f.Kind = BallKind.Fragment;
        f.fuse = FragmentFuse;
        f.SourceId = GameManager.Instance.NextSourceId();
        f.sr.sprite = SpriteHelper.Rainbow;
        f.sr.sortingOrder = 2;
        f.AddTrail(Look.SparkTrail, 0.25f);
        // 火星从出生起就是"活着的火"，连锁要等它烧完才算结束
        GameManager.Instance.ReactionStarted();
        return f;
    }

    void AddTrail(Color c, float time)
    {
        var trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = time;
        trail.startWidth = currentScale * 0.5f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(c.r, c.g, c.b, 0.7f);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
        trail.sortingOrder = 0;
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

        if (Kind == BallKind.Fragment && CurrentState == BallState.Moving)
        {
            IgniteByContact();
            fuse -= Time.deltaTime;
            if (fuse <= 0f) Pop();
            return;
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
        AddTrail(sr.color, 0.4f);
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
                other.Hit(SourceId, transform.position);
        }
    }

    /// <summary>火星在飞行中碰到的球直接点着 —— 碰到就算，不用等膨胀</summary>
    void IgniteByContact()
    {
        float myRadius = currentScale * 0.5f;
        var balls = GameManager.Instance.AllBalls;
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            var other = balls[i];
            if (other == this || other.Kind == BallKind.Fragment) continue;
            if (other.CurrentState != BallState.Moving && other.CurrentState != BallState.Fleeing) continue;
            float dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist < myRadius + other.currentScale * 0.5f)
                other.Hit(SourceId, transform.position);
        }
    }

    /// <summary>
    /// 一把火碰到了这颗球。普通球直接炸；火星不吃别人的火（它自己就是火）。
    /// 硬球是两层：第一次被火碰到，外壳"啪"地碎掉（不传火），剩下一颗普通球；
    /// 打壳那一波火（壳碎后 HardCooldown 秒内碰到它的每一把）都作废，要等之后再来的火才炸。
    /// 所以硬球要被"打两次"：火得从另一条路绕回来，或者靠火星、移动的球把第二把火送到。
    /// </summary>
    public void Hit(int sourceId, Vector2 from)
    {
        if (CurrentState != BallState.Moving && CurrentState != BallState.Fleeing) return;
        if (Kind == BallKind.Fragment) return;

        if (Kind == BallKind.Hard)
        {
            if (crackedAt < 0f)
            {
                crackedAt = Time.time;
                spentSources.Add(sourceId);
                Crack();
                return;
            }
            if (spentSources.Contains(sourceId)) return;
            if (Time.time - crackedAt < HardCooldown)
            {
                spentSources.Add(sourceId);
                return;
            }
        }

        Trigger(from);
    }

    void Crack()
    {
        sr.sprite = SpriteHelper.Circle;
        ShellBurst.Create(transform.position, currentScale, sr.color);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    public void Trigger() => Trigger(transform.position);

    public void Trigger(Vector2 from)
    {
        if (CurrentState != BallState.Moving && CurrentState != BallState.Fleeing) return;
        CurrentState = BallState.Triggered;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        triggerTimer = reactionDuration;
        if (SourceId == 0) SourceId = GameManager.Instance.NextSourceId();

        if (IsGoldBall)
        {
            // 金球不画膨胀圈：金币的颜色是烘进贴图里的，SpriteRenderer.color 本身是白色，
            // 换成纯圆之后那个白色就露出来，成了一个刺眼的半透明白圈。
            // 直接关掉渲染，只留 GoldBurst 那个美元符号当"影子"。
            // 检测范围不受影响 —— 膨胀是 currentScale 在算，和渲染无关。
            sr.enabled = false;
            GoldBurst.Create(transform.position, GoldColor, currentScale);
        }

        // 着火的样子由外观层定：白模里换成强调色，彩色模式下是原色变半透明
        sr.color = Look.Burn(sr.color, GameManager.Instance.IsDarkBackground);
        GameManager.Instance.OnBallTriggered(this);
        GameManager.Instance.ReactionStarted();

        if (Kind == BallKind.Split) SpawnFragments(from);
    }

    /// <summary>火星到时间自己炸一下小的。不算通关数，ReactionStarted 在出生时已经记过</summary>
    void Pop()
    {
        CurrentState = BallState.Triggered;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        triggerTimer = reactionDuration;
        var trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.emitting = false;
        var c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, 0.45f);
    }

    void SpawnFragments(Vector2 from)
    {
        Vector2 pos = transform.position;
        Vector2 a, b;
        if (IsAimed) AimDirections(out a, out b);
        else
        {
            Vector2 forward = pos - from;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector2.up;
            forward.Normalize();
            a = Rotate(forward, SplitHalfAngleDeg);
            b = Rotate(forward, -SplitHalfAngleDeg);
        }
        CreateFragment(pos, a, currentScale, reactionDuration);
        CreateFragment(pos, b, currentScale, reactionDuration);
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
