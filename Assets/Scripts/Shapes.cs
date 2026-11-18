using UnityEngine;

/// <summary>
/// 用网格画的平涂形状：环带的一段（硬球的壳）、小多边形（碎石）。
/// 贴图环缩放时粗细跟着变，LineRenderer 画不出棱角分明的拐角，所以这两样用网格。
/// 都用 Sprites/Default，和 SpriteRenderer 走同一套 sortingOrder。
/// </summary>
public static class Shapes
{
    /// <summary>正六边形在角度 deg 方向上的"半径"（apothem 是边心距）。flat = 上下是平边</summary>
    public static float HexRadius(float apothem, float deg)
    {
        float a = Mathf.Repeat(deg, 60f) - 30f;
        return apothem / Mathf.Cos(a * Mathf.Deg2Rad);
    }

    /// <summary>
    /// 环带的一段：从 startDeg 起逆时针 sweepDeg 度，里边界半径 inner、外边界半径 outer。
    /// hexInner / hexOuter 为真时，那条边界是正六边形（半径当边心距用），否则是圆。
    /// </summary>
    public static MeshRenderer Band(Transform parent, string name, float startDeg, float sweepDeg,
        float inner, float outer, bool hexInner, bool hexOuter, Color color, int order)
    {
        int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(sweepDeg) / 2.5f) + 1);
        var verts = new Vector3[n * 2];
        var cols = new Color[n * 2];
        var tris = new int[(n - 1) * 6];
        for (int i = 0; i < n; i++)
        {
            float deg = startDeg + sweepDeg * i / (n - 1);
            float r = deg * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(r), Mathf.Sin(r), 0f);
            verts[i * 2] = dir * (hexInner ? HexRadius(inner, deg) : inner);
            verts[i * 2 + 1] = dir * (hexOuter ? HexRadius(outer, deg) : outer);
            cols[i * 2] = cols[i * 2 + 1] = Color.white;
            if (i < n - 1)
            {
                int t = i * 6, v = i * 2;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
            }
        }
        return Make(parent, name, verts, cols, tris, color, order);
    }

    /// <summary>一块凸多边形（顶点按顺序给，局部坐标）</summary>
    public static MeshRenderer Poly(Transform parent, string name, Vector2[] pts, Color color, int order)
    {
        var verts = new Vector3[pts.Length];
        var cols = new Color[pts.Length];
        for (int i = 0; i < pts.Length; i++) { verts[i] = pts[i]; cols[i] = Color.white; }
        var tris = new int[(pts.Length - 2) * 3];
        for (int i = 0; i < pts.Length - 2; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }
        return Make(parent, name, verts, cols, tris, color, order);
    }

    static MeshRenderer Make(Transform parent, string name, Vector3[] verts, Color[] cols, int[] tris, Color color, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        var mesh = new Mesh { vertices = verts, colors = cols, triangles = tris };
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = RingLine.Mat;
        mr.sortingOrder = order;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        SetColor(mr, color);
        return mr;
    }

    static MaterialPropertyBlock block;

    /// <summary>改颜色不生成新材质（Sprites/Default 的 _Color 乘在顶点色上）</summary>
    public static void SetColor(MeshRenderer mr, Color c)
    {
        if (block == null) block = new MaterialPropertyBlock();
        mr.GetPropertyBlock(block);
        block.SetColor("_Color", c);
        // Sprites/Default 没有贴图时要一张白图，否则采样出来是黑的
        block.SetTexture("_MainTex", Texture2D.whiteTexture);
        mr.SetPropertyBlock(block);
    }
}
