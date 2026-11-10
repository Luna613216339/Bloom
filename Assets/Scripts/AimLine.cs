using UnityEngine;

/// <summary>
/// 定向分裂球的瞄准虚线：从五彩球画出两条点线，照着火星真正会飞的路线描 ——
/// 同样的出生点、同样的半径、同样的射程，碰到墙和关着的门按镜面反射折过去，开着的门直接穿过。
/// 每帧重算，所以开关门、五彩球自己在动（流星）时线会跟着变。
///
/// 为什么要有它：火星往哪飞以前是看不见的，玩家只能点了再看，过不了就瞎猜。
/// 有了线，「门要不要关」「什么时候点」都能在点之前想清楚。背着火源飞的那种分裂球没有线（方向取决于火从哪来）。
/// 开关在 Look.AimLines。
/// </summary>
public class AimLine : MonoBehaviour
{
    const float Width = 0.09f;
    const float Period = 0.25f;         // 相邻两个点的间距
    const int MaxBounces = 2;
    // 墙、门、笼子在 Default 层，屏幕边界在 Ignore Raycast 层；球在第 6 层，不挡火星
    const int SolidMask = (1 << 0) | (1 << 2);

    static Material dotMaterial;
    static readonly RaycastHit2D[] hits = new RaycastHit2D[4];

    const int ScreenBoundLayer = 2;

    Ball ball;
    // 每个方向最多 MaxBounces + 1 段，每段一条独立的线：整条折线用一个 LineRenderer 画的话，锐角拐弯处的点会被拉花
    LineRenderer[] segsA, segsB;
    ContactFilter2D filter;

    public void Configure(Ball owner)
    {
        ball = owner;
        filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = SolidMask };
        segsA = MakeLines("AimLineA");
        segsB = MakeLines("AimLineB");
    }

    LineRenderer[] MakeLines(string name)
    {
        var arr = new LineRenderer[MaxBounces + 1];
        for (int i = 0; i < arr.Length; i++) arr[i] = MakeLine(name + i);
        return arr;
    }

    LineRenderer MakeLine(string name)
    {
        // 不挂在球下面：球被点着以后会放大，子物体的线会跟着变粗
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = Width;
        lr.endWidth = Width;
        lr.textureMode = LineTextureMode.Tile;
        // Tile 模式默认每个世界单位铺一格；Sprites/Default 不认材质上的 tiling，只能用线自己的 textureScale
        lr.textureScale = new Vector2(1f / Period, 1f);
        lr.sharedMaterial = DotMaterial;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.alignment = LineAlignment.View;
        var c = Look.Aim;
        lr.startColor = c;
        lr.endColor = c;
        lr.sortingOrder = -1;
        lr.positionCount = 0;
        return lr;
    }

    /// <summary>一排圆点的贴图：正中一个圆，两边留空，沿线平铺</summary>
    static Material DotMaterial
    {
        get
        {
            if (dotMaterial != null) return dotMaterial;
            const int h = 32;
            int w = Mathf.RoundToInt(h * Period / Width);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float r = h * 0.5f - 1.5f;
            var center = new Vector2(w * 0.5f, h * 0.5f);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
                }
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            dotMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
            return dotMaterial;
        }
    }

    void LateUpdate()
    {
        if (ball == null || ball.CurrentState != Ball.BallState.Moving)
        {
            // 着火了：火星已经飞出去，线的任务结束
            Clear();
            Destroy(this);
            return;
        }
        ball.AimDirections(out var a, out var b);
        Trace(segsA, a);
        Trace(segsB, b);
    }

    void Trace(LineRenderer[] segs, Vector2 dir)
    {
        Vector2 pos = (Vector2)ball.transform.position + dir * ball.FragmentSpawnOffset;
        float radius = ball.FragmentRadius;
        float left = Ball.FragmentRange;
        int n = 0;
        while (n < segs.Length && left > 0.05f)
        {
            int count = Physics2D.CircleCast(pos, radius, dir, filter, hits, left);
            Vector2 end = count == 0 ? pos + dir * left : hits[0].centroid;
            // 圆点落在一张沿这个方向的固定格子上：第一段起点往回退（藏在球底下），折过以后的段往前挪到下一个格点。
            // 几颗五彩球排在一条线上时（接力），各自的虚线叠在一起，点对不齐会糊成一条乱线
            float phase = Mathf.Repeat(Vector2.Dot(pos, dir), Period);
            Vector2 start = n == 0 ? pos - dir * phase : pos + dir * (Period - phase);
            if (Vector2.Dot(end - start, dir) > 0.01f)
            {
                segs[n].positionCount = 2;
                segs[n].SetPosition(0, start);
                segs[n].SetPosition(1, end);
                n++;
            }
            if (count == 0) break;
            // 火星碰到屏幕边也会弹，但那一段从来没用，画出来只是在边上叠成一团
            if (hits[0].collider.gameObject.layer == ScreenBoundLayer) break;
            left -= hits[0].distance;
            dir = Vector2.Reflect(dir, hits[0].normal);
            // 往反射方向挪一丝，下一段不会又撞在同一面墙上
            pos = end + dir * 0.01f;
        }
        for (int i = n; i < segs.Length; i++) segs[i].positionCount = 0;
    }

    void Clear()
    {
        foreach (var arr in new[] { segsA, segsB })
        {
            if (arr == null) continue;
            foreach (var lr in arr) if (lr != null) Destroy(lr.gameObject);
        }
        segsA = null; segsB = null;
    }

    void OnDestroy() => Clear();
}
