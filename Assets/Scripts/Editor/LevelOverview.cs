using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把全部关卡的初始布局画成一张总览图（Docs/关卡总览.png），一眼看出有没有两关长得像。
/// 纯 CPU 画，不开相机，所以命令行 -batchmode 也能跑：
///   Unity -batchmode -quit -projectPath . -executeMethod LevelOverview.Render
///
/// 随机散布的关（每次进关重新随机）按固定种子画一个代表性布局，右上角标一个 R。
/// 蛇形关按 t=0 的队形画，并把路径画成淡线。
/// </summary>
public static class LevelOverview
{
    const int W = 384, H = 216;         // 每格 16:9
    const int Cols = 5;
    const int Pad = 10;
    const float HalfW = 5f * 16f / 9f, HalfH = 5f;

    static readonly Color Bg = Color.white;
    static readonly Color Frame = new Color(0.8f, 0.8f, 0.8f);
    static readonly Color Ink = new Color(0.2f, 0.2f, 0.2f);
    static readonly Color WallColor = new Color(0.22f, 0.22f, 0.22f);
    static readonly Color PathColor = new Color(0.3f, 0.6f, 1f, 0.35f);
    static readonly Color EscapeColor = new Color(0.25f, 0.08f, 0.35f);
    static readonly Color DoorOpenColor = new Color(0.6f, 0.6f, 0.6f);

    [MenuItem("Tools/Bloom/Render Level Overview")]
    public static void Render()
    {
        var levels = LevelCatalog.All;
        int rows = (levels.Count + Cols - 1) / Cols;
        int fullW = Cols * (W + Pad) + Pad;
        int fullH = rows * (H + Pad) + Pad;
        var tex = new Texture2D(fullW, fullH, TextureFormat.RGBA32, false);
        Fill(tex, new Color(0.94f, 0.94f, 0.94f));

        for (int i = 0; i < levels.Count; i++)
        {
            int col = i % Cols;
            int row = i / Cols;
            int ox = Pad + col * (W + Pad);
            int oy = fullH - Pad - (row + 1) * H - row * Pad;   // 纹理 y 向上，所以第一行在最上面
            DrawLevel(tex, ox, oy, levels[i]);
        }

        tex.Apply();
        string root = Directory.GetParent(Application.dataPath).FullName;
        string path = Path.Combine(root, "Docs", "关卡总览.png");
        File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
        Object.DestroyImmediate(tex);
        Debug.Log("[Bloom] 关卡总览已写到 " + path);
    }

    // ---- 单关 ----

    static void DrawLevel(Texture2D tex, int ox, int oy, LevelDef d)
    {
        var cell = new Cell { tex = tex, ox = ox, oy = oy };
        cell.FillRect(0, 0, W, H, Bg);

        var palette = Look.Palette(d.Palette);
        foreach (var w in d.Walls) cell.Obb(w.Center, w.Size * 0.5f, w.Rot, Look.Structure(WallColor));
        foreach (var c in d.Cages) DrawCage(cell, c);
        foreach (var door in d.Doors)
            cell.Obb(door.Center, door.Size * 0.5f, door.Rot,
                door.StartsOpen ? DoorOpenColor : Look.Door(door.Link != 0, door.Color),
                hollow: door.StartsOpen);

        foreach (var s in d.Schools) DrawSchool(cell, s, d);

        int colorIndex = 0;
        foreach (var b in d.Balls)
        {
            float size = b.Size > 0f ? b.Size : d.BallSize;
            cell.Ball(b.Pos, size * 0.5f, BallPalette.Cycle(palette, colorIndex++), b.Kind);
        }

        foreach (var sc in d.Scatter)
        {
            var rng = new System.Random(sc.Seed < 0 ? 1 : sc.Seed);
            for (int i = 0; i < sc.Count; i++)
            {
                float x = Mathf.Lerp(sc.Region.xMin, sc.Region.xMax, (float)rng.NextDouble());
                float y = Mathf.Lerp(sc.Region.yMin, sc.Region.yMax, (float)rng.NextDouble());
                rng.NextDouble();   // 对齐 LevelBuilder 里取方向的那一次
                cell.Ball(new Vector2(x, y), d.BallSize * 0.5f, BallPalette.Cycle(palette, colorIndex++), sc.Kind);
            }
        }

        // 边框、关号、随机标记
        cell.Border(Frame);
        cell.Number(d.Number, 8, H - 8 - 7 * 3, Ink, 3);
        int gx = W - 8 - 5 * 3;
        if (d.HasRandomLayout) { cell.Glyph(Glyphs.R, gx, H - 8 - 7 * 3, Ink, 3); gx -= 20; }
        string[] mark = d.Pool == LevelPool.Sample ? Glyphs.S : d.Pool == LevelPool.Candidate ? Glyphs.C
            : d.Pool == LevelPool.Experiment ? Glyphs.E : null;
        if (mark != null) cell.Glyph(mark, gx, H - 8 - 7 * 3, Ink, 3);
        if (d.Idle != null && d.Idle.Mode != IdleMode.None) cell.Glyph(Glyphs.I, gx - 14, H - 8 - 7 * 3, Ink, 3);
    }

    static void DrawCage(Cell cell, CageDef c)
    {
        float t = c.Thickness;
        float fullW = c.Width + t;
        var rot = Quaternion.Euler(0f, 0f, c.Rot);
        Vector2 L(Vector2 local) => c.Center + (Vector2)(rot * (Vector3)local);

        var cc = Look.Structure(c.Color);
        cell.Obb(L(new Vector2(0, c.Height / 2f)), new Vector2(fullW, t) * 0.5f, c.Rot, cc);
        cell.Obb(L(new Vector2(0, -c.Height / 2f)), new Vector2(fullW, t) * 0.5f, c.Rot, cc);
        Side(-c.Width / 2f, c.LeftDoor);
        Side(c.Width / 2f, c.RightDoor);

        void Side(float x, bool hasDoor)
        {
            if (hasDoor)
            {
                float seg = (c.Height - c.DoorGap) / 2f;
                float off = (c.DoorGap + seg) / 2f;
                cell.Obb(L(new Vector2(x, off)), new Vector2(t, seg) * 0.5f, c.Rot, cc);
                cell.Obb(L(new Vector2(x, -off)), new Vector2(t, seg) * 0.5f, c.Rot, cc);
            }
            else
                cell.Obb(L(new Vector2(x, 0)), new Vector2(t, c.Height) * 0.5f, c.Rot, cc);
        }
    }

    /// <summary>按 SchoolSpawner 的公式算 t=0 的队形，和游戏里第一帧一致</summary>
    static void DrawSchool(Cell cell, SchoolDef s, LevelDef d)
    {
        var path = new SchoolPath(s);
        // 路径线
        if (s.Circle)
        {
            for (int i = 0; i < 96; i++)
                cell.Line(path.At(path.Total * i / 96f), path.At(path.Total * (i + 1) / 96f), PathColor);
        }
        else if (s.Waypoints != null)
        {
            for (int i = 0; i < s.Waypoints.Length - 1; i++)
                cell.Line(s.Waypoints[i], s.Waypoints[i + 1], PathColor);
        }

        float head = (s.Count - 1) * s.Spacing + s.StartOffset;
        int normal = s.Count - s.Escape;
        var palette = Look.Palette(s.Palette ?? d.Palette, s.LightTone);
        for (int i = 0; i < s.Count; i++)
        {
            float dist = head - i * s.Spacing;
            Vector2 pos = path.At(dist);
            if (s.Wave)
            {
                Vector2 dir = (path.At(dist + 0.01f) - path.At(dist - 0.01f)).normalized;
                pos += Vector2.Perpendicular(dir) * (Mathf.Sin(i * s.Spacing * 2f) * s.WaveAmplitude);
            }
            bool escape = i >= normal;
            var kind = s.Kinds != null && i < s.Kinds.Length ? s.Kinds[i] : BallKind.Normal;
            cell.Ball(pos, s.BallSize * 0.5f, escape ? Look.Escape(EscapeColor) : BallPalette.Ramp(palette, i, normal), kind);
        }
    }

    class SchoolPath
    {
        readonly SchoolDef s;
        readonly float[] seg;
        public readonly float Total;

        public SchoolPath(SchoolDef s)
        {
            this.s = s;
            if (s.Circle) { Total = 2f * Mathf.PI * s.Radius; return; }
            int n = s.Loop ? s.Waypoints.Length : s.Waypoints.Length - 1;
            seg = new float[n];
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % s.Waypoints.Length;
                seg[i] = Vector2.Distance(s.Waypoints[i], s.Waypoints[next]);
                Total += seg[i];
            }
        }

        public Vector2 At(float dist)
        {
            if (s.Circle)
            {
                float a = dist / s.Radius;
                if (s.Clockwise) a = -a;
                a += s.PhaseDeg * Mathf.Deg2Rad;
                return s.Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s.Radius;
            }
            if (s.Loop) { dist %= Total; if (dist < 0) dist += Total; }
            else dist = Mathf.Clamp(dist, 0, Total);
            float acc = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                if (acc + seg[i] >= dist)
                {
                    int next = (i + 1) % s.Waypoints.Length;
                    return Vector2.Lerp(s.Waypoints[i], s.Waypoints[next], (dist - acc) / seg[i]);
                }
                acc += seg[i];
            }
            return s.Waypoints[s.Waypoints.Length - 1];
        }
    }

    // ---- 像素画板 ----

    class Cell
    {
        public Texture2D tex;
        public int ox, oy;
        const float Scale = W / (2f * HalfW);

        int Px(float wx) => Mathf.RoundToInt((wx + HalfW) * Scale);
        int Py(float wy) => Mathf.RoundToInt((wy + HalfH) * Scale);

        void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            if (c.a < 1f)
            {
                var old = tex.GetPixel(ox + x, oy + y);
                c = Color.Lerp(old, new Color(c.r, c.g, c.b, 1f), c.a);
            }
            tex.SetPixel(ox + x, oy + y, c);
        }

        public void FillRect(int x0, int y0, int w, int h, Color c)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++) Set(x, y, c);
        }

        public void Border(Color c)
        {
            for (int x = 0; x < W; x++) { Set(x, 0, c); Set(x, H - 1, c); }
            for (int y = 0; y < H; y++) { Set(0, y, c); Set(W - 1, y, c); }
        }

        public void Ball(Vector2 p, float r, Color c, BallKind kind)
        {
            int cx = Px(p.x), cy = Py(p.y);
            int pr = Mathf.Max(2, Mathf.RoundToInt(r * Scale));
            for (int y = -pr; y <= pr; y++)
                for (int x = -pr; x <= pr; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d > pr + 0.5f) continue;
                    Color col = c;
                    if (kind == BallKind.Split)
                    {
                        float ang = Mathf.Atan2(y, x);
                        if (ang < 0) ang += Mathf.PI * 2f;
                        col = Color.HSVToRGB(Mathf.Floor(ang / (Mathf.PI * 2f) * 6f) / 6f, 0.8f, 0.92f);
                    }
                    else if (kind == BallKind.Hard && d > pr * 0.55f && d < pr * 0.78f)
                        col = Color.Lerp(c, Color.white, 0.85f);
                    Set(cx + x, cy + y, col);
                }
        }

        public void Obb(Vector2 center, Vector2 half, float rotDeg, Color c, bool hollow = false)
        {
            float rad = -rotDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            float ext = Mathf.Max(half.x, half.y) + 0.1f;
            int x0 = Px(center.x - ext), x1 = Px(center.x + ext);
            int y0 = Py(center.y - ext), y1 = Py(center.y + ext);
            float edge = 1.5f / Scale;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float wx = x / Scale - HalfW - center.x;
                    float wy = y / Scale - HalfH - center.y;
                    float lx = wx * cs - wy * sn;
                    float ly = wx * sn + wy * cs;
                    bool inside = Mathf.Abs(lx) <= half.x && Mathf.Abs(ly) <= half.y;
                    if (!inside) continue;
                    if (hollow && Mathf.Abs(lx) < half.x - edge && Mathf.Abs(ly) < half.y - edge) continue;
                    Set(x, y, c);
                }
        }

        public void Line(Vector2 a, Vector2 b, Color c)
        {
            int x0 = Px(a.x), y0 = Py(a.y), x1 = Px(b.x), y1 = Py(b.y);
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Set(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), c);
            }
        }

        public void Number(int n, int x, int y, Color c, int s)
        {
            string str = n.ToString();
            foreach (char ch in str)
            {
                Glyph(Glyphs.Digit(ch - '0'), x, y, c, s);
                x += 4 * s;
            }
        }

        public void Glyph(string[] g, int x, int y, Color c, int s)
        {
            for (int row = 0; row < g.Length; row++)
                for (int col = 0; col < g[row].Length; col++)
                    if (g[row][col] == '#')
                        FillRect(x + col * s, y + (g.Length - 1 - row) * s, s, s, c);
        }
    }

    static class Glyphs
    {
        static readonly string[][] Digits =
        {
            new[]{"###","#.#","#.#","#.#","#.#","#.#","###"},
            new[]{"..#","..#","..#","..#","..#","..#","..#"},
            new[]{"###","..#","..#","###","#..","#..","###"},
            new[]{"###","..#","..#","###","..#","..#","###"},
            new[]{"#.#","#.#","#.#","###","..#","..#","..#"},
            new[]{"###","#..","#..","###","..#","..#","###"},
            new[]{"###","#..","#..","###","#.#","#.#","###"},
            new[]{"###","..#","..#","..#","..#","..#","..#"},
            new[]{"###","#.#","#.#","###","#.#","#.#","###"},
            new[]{"###","#.#","#.#","###","..#","..#","###"},
        };
        public static string[] Digit(int d) => Digits[Mathf.Clamp(d, 0, 9)];
        public static readonly string[] R = {"##.","#.#","#.#","##.","#.#","#.#","#.#"};
        public static readonly string[] S = {".##","#..","#..",".#.","..#","..#","##."};
        public static readonly string[] C = {".##","#..","#..","#..","#..","#..",".##"};
        public static readonly string[] E = {"###","#..","#..","##.","#..","#..","###"};
        public static readonly string[] I = {"###",".#.",".#.",".#.",".#.",".#.","###"};
    }

    static void Fill(Texture2D tex, Color c)
    {
        var px = new Color[tex.width * tex.height];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        tex.SetPixels(px);
    }
}
