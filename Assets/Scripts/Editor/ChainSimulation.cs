using UnityEngine;

/// <summary>
/// 游戏规则的纯数学副本，不依赖 Unity 物理引擎，一局不到一毫秒。
///
/// 能这么写的前提（已核对过代码）：
/// 1. 球之间互相穿过（Ball.SetupPhysics 里 IgnoreLayerCollision），所以球的运动
///    就是匀速直线 + 撞墙反弹，不用算球球碰撞
/// 2. 连锁检测是手动距离判断（Ball.DetectNearbyBalls），不走物理触发
///
/// 任何时候改了 Ball.cs 或 ClickReaction.cs 的判定逻辑，这里要跟着改，
/// 否则模拟器会悄悄地和真实游戏对不上。
/// </summary>
public class ChainSimulation
{
    const float Dt = 1f / 60f;              // 游戏里的膨胀走 Time.deltaTime，按 60fps 算
    const float ClickExpandSpeed = 5f;      // ClickReaction.ExpandSpeed
    const float BallExpandSpeed = 5f;       // Ball.Create 的 expandSpeed 默认值
    const float SpawnMargin = 0.5f;         // GameManager.SpawnBalls
    const float GameAspect = 16f / 9f;
    const int MaxSteps = 60 * 60;           // 保险丝，正常一局 3 秒内结束

    readonly float halfW, halfH;
    readonly float ballSpeed, ballRadius, maxScale, duration;

    // 0 = Moving, 1 = Triggered, 2 = 烧完了
    float[] px, py, vx, vy, scale, timer;
    int[] state;

    public ChainSimulation(float orthoSize, float ballSpeed, float ballSize,
        float reactionMaxScale, float reactionDuration)
    {
        halfH = orthoSize;
        halfW = orthoSize * GameAspect;
        this.ballSpeed = ballSpeed;
        this.ballRadius = ballSize * 0.5f;
        this.maxScale = reactionMaxScale;
        this.duration = reactionDuration;
    }

    void Alloc(int n)
    {
        if (px != null && px.Length >= n) return;
        px = new float[n]; py = new float[n];
        vx = new float[n]; vy = new float[n];
        scale = new float[n]; timer = new float[n];
        state = new int[n];
    }

    /// <summary>撒一局球。返回球数，位置存在内部数组里</summary>
    public void Spawn(int ballCount, System.Random rng)
    {
        Alloc(ballCount);
        for (int i = 0; i < ballCount; i++)
        {
            px[i] = Lerp(-halfW + SpawnMargin, halfW - SpawnMargin, (float)rng.NextDouble());
            py[i] = Lerp(-halfH + SpawnMargin, halfH - SpawnMargin, (float)rng.NextDouble());
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            vx[i] = Mathf.Cos(a) * ballSpeed;
            vy[i] = Mathf.Sin(a) * ballSpeed;
            scale[i] = ballRadius * 2f;
            timer[i] = 0f;
            state[i] = 0;
        }
    }

    public void GetBall(int i, out float x, out float y) { x = px[i]; y = py[i]; }

    /// <summary>在当前这局球上点一下，跑完整条连锁，返回被触发的球数</summary>
    public int Resolve(int ballCount, float clickX, float clickY)
    {
        float clickScale = 0f;
        float clickTimer = duration;
        bool clickAlive = true;
        int triggered = 0;

        for (int step = 0; step < MaxSteps; step++)
        {
            // 移动还没被触发的球，撞到屏幕边界反弹
            for (int i = 0; i < ballCount; i++)
            {
                if (state[i] != 0) continue;
                px[i] += vx[i] * Dt;
                py[i] += vy[i] * Dt;

                float limX = halfW - ballRadius;
                float limY = halfH - ballRadius;
                if (px[i] < -limX) { px[i] = -limX; vx[i] = -vx[i]; }
                else if (px[i] > limX) { px[i] = limX; vx[i] = -vx[i]; }
                if (py[i] < -limY) { py[i] = -limY; vy[i] = -vy[i]; }
                else if (py[i] > limY) { py[i] = limY; vy[i] = -vy[i]; }
            }

            // 玩家点出来的那个圈
            if (clickAlive)
            {
                clickScale = MoveTowards(clickScale, maxScale, ClickExpandSpeed * Dt);
                triggered += Detect(ballCount, clickX, clickY, clickScale * 0.5f);
                clickTimer -= Dt;
                if (clickTimer <= 0f) clickAlive = false;
            }

            // 已经被触发的球，一边膨胀一边继续点燃别人
            bool anyActive = clickAlive;
            for (int i = 0; i < ballCount; i++)
            {
                if (state[i] != 1) continue;
                anyActive = true;
                scale[i] = MoveTowards(scale[i], maxScale, BallExpandSpeed * Dt);
                triggered += Detect(ballCount, px[i], py[i], scale[i] * 0.5f);
                timer[i] -= Dt;
                if (timer[i] <= 0f) state[i] = 2;
            }

            if (!anyActive) break;
        }

        return triggered;
    }

    int Detect(int ballCount, float cx, float cy, float radius)
    {
        int count = 0;
        for (int i = 0; i < ballCount; i++)
        {
            if (state[i] != 0) continue;
            float dx = px[i] - cx;
            float dy = py[i] - cy;
            float r = radius + ballRadius;
            if (dx * dx + dy * dy < r * r)
            {
                state[i] = 1;
                timer[i] = duration;
                count++;
            }
        }
        return count;
    }

    /// <summary>"点看上去最密的地方"——用来验证随机点击是不是够用的基准</summary>
    public void FindDensestPoint(int ballCount, System.Random rng, int samples,
        out float bestX, out float bestY)
    {
        float catchRadius = maxScale * 0.5f + ballRadius;
        float r2 = catchRadius * catchRadius;
        bestX = 0f; bestY = 0f;
        int best = -1;

        for (int s = 0; s < samples; s++)
        {
            float cx = Lerp(-halfW, halfW, (float)rng.NextDouble());
            float cy = Lerp(-halfH, halfH, (float)rng.NextDouble());
            int c = 0;
            for (int i = 0; i < ballCount; i++)
            {
                float dx = px[i] - cx;
                float dy = py[i] - cy;
                if (dx * dx + dy * dy < r2) c++;
            }
            if (c > best) { best = c; bestX = cx; bestY = cy; }
        }
    }

    public void RandomPoint(System.Random rng, out float x, out float y)
    {
        x = Lerp(-halfW, halfW, (float)rng.NextDouble());
        y = Lerp(-halfH, halfH, (float)rng.NextDouble());
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    static float MoveTowards(float current, float target, float delta)
    {
        if (current + delta >= target) return target;
        return current + delta;
    }
}
