using UnityEngine;

public static class SpriteHelper
{
    private static Sprite _circle;
    private static Sprite _square;
    private static Sprite _crosshatch;
    private static Sprite _stripedCircle;
    private static Sprite _coin;
    private static Sprite _dollar;

    public static Sprite Circle
    {
        get
        {
            if (_circle == null)
                _circle = CreateCircleSprite(256);
            return _circle;
        }
    }

    public static Sprite Square
    {
        get
        {
            if (_square == null)
                _square = CreateSquareSprite(32);
            return _square;
        }
    }

    public static Sprite Crosshatch
    {
        get
        {
            if (_crosshatch == null)
                _crosshatch = CreateCrosshatchSprite(64);
            return _crosshatch;
        }
    }

    /// <summary>
    /// 金币。低画质下金属感不靠渐变，靠硬边分区的三段明暗：
    /// 深金外环 → 亮金内盘 → 更深的符号，色相全部同源，只差明度。
    /// 不用黑色描边 —— 黑边会让它看起来像贴纸，不像同一块金属。
    /// </summary>
    public static Sprite Coin
    {
        get
        {
            if (_coin == null)
                _coin = CreateCoinSprite(256);
            return _coin;
        }
    }

    /// <summary>单独的美元符号，白色，用 SpriteRenderer 染色</summary>
    public static Sprite Dollar
    {
        get
        {
            if (_dollar == null)
                _dollar = CreateDollarSprite(256);
            return _dollar;
        }
    }

    // 美元符号的点阵，7 宽 11 高。低分辨率手写反而比算法生成的曲线更好看
    static readonly string[] DollarGlyph =
    {
        "...#...",
        ".#####.",
        "#..#..#",
        "#..#...",
        ".#.#...",
        "..###..",
        "...#.#.",
        "...#..#",
        "#..#..#",
        ".#####.",
        "...#...",
    };

    static bool GlyphAt(float u, float v)
    {
        // u,v 是 0..1 的符号区域坐标，v 向上
        int gx = Mathf.FloorToInt(u * DollarGlyph[0].Length);
        int gy = Mathf.FloorToInt((1f - v) * DollarGlyph.Length);
        if (gx < 0 || gy < 0 || gx >= DollarGlyph[0].Length || gy >= DollarGlyph.Length)
            return false;
        return DollarGlyph[gy][gx] == '#';
    }

    static Sprite CreateCoinSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radius = center - 1f;
        float rimInner = radius * 0.80f;

        var rim = new Color(0.80f, 0.54f, 0.11f);      // 外环，深金
        var disc = new Color(1.00f, 0.81f, 0.26f);     // 内盘，亮金
        var sheen = new Color(1.00f, 0.94f, 0.66f);    // 斜向高光条
        var glyph = new Color(0.60f, 0.36f, 0.04f);    // 符号，最深

        float glyphHalf = radius * 0.42f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - dist);
                if (alpha <= 0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                Color c = dist > rimInner ? rim : disc;

                // 45 度亮条：金属和塑料唯一的区别信号
                if (dist <= rimInner)
                {
                    float band = dx + dy;
                    float w = radius * 0.16f;
                    float off = radius * 0.34f;
                    if (band > off - w && band < off + w)
                        c = Color.Lerp(c, sheen, 0.75f);
                }

                // 中间的美元符号
                float u = (dx + glyphHalf) / (glyphHalf * 2f);
                float v = (dy + glyphHalf) / (glyphHalf * 2f);
                if (u >= 0f && u <= 1f && v >= 0f && v <= 1f && GlyphAt(u, v))
                    c = glyph;

                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateDollarSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)resolution;
                float v = y / (float)resolution;
                tex.SetPixel(x, y, GlyphAt(u, v) ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateCircleSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radius = center - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    public static Sprite StripedCircle
    {
        get
        {
            if (_stripedCircle == null)
                _stripedCircle = CreateStripedCircleSprite(256);
            return _stripedCircle;
        }
    }

    static Sprite CreateStripedCircleSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radius = center - 1f;
        int stripeWidth = resolution / 12;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist);
                if (alpha <= 0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool onStripe = (x + y) % (stripeWidth * 2) < stripeWidth;
                float brightness = onStripe ? 1f : 0.35f;
                tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateSquareSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateCrosshatchSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        int spacing = resolution / 6;
        int lineWidth = Mathf.Max(1, resolution / 32);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                bool onLine = (x + y) % spacing < lineWidth
                           || (x - y + resolution * 2) % spacing < lineWidth;
                tex.SetPixel(x, y, onLine ? Color.white : new Color(1f, 1f, 1f, 0.3f));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }
}
