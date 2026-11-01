using UnityEngine;

public static class SpriteHelper
{
    private static Sprite _circle;
    private static Sprite _square;
    private static Sprite _crosshatch;
    private static Sprite _stripedCircle;
    private static Sprite _coin;
    private static Sprite _dollar;
    private static Texture2D _banknote;
    private static Sprite _rainbow;
    private static Sprite _shell;
    private static Sprite _ring;

    /// <summary>
    /// 分裂球 / 火星：六个色相扇区拼成的五彩球。颜色烤在贴图里，不跟关卡配色走 ——
    /// 它的任务是在任何配色里都一眼认出"这颗不一样"，和逃跑球的条纹是同一个道理。
    /// 饱和度压到 0.8、明度 0.92，否则黄色那一瓣在白底上会飘。
    /// </summary>
    public static Sprite Rainbow
    {
        get
        {
            if (_rainbow == null)
                _rainbow = CreateRainbowSprite(256);
            return _rainbow;
        }
    }

    /// <summary>
    /// 硬球：实心核 + 一圈透明缝 + 实心外壳。缝里透出背景色，看起来就是"外面套了一层"。
    /// 整张图是白的，颜色由 SpriteRenderer 染，所以仍然跟关卡配色。壳被敲裂后换回普通圆。
    /// </summary>
    public static Sprite Shell
    {
        get
        {
            if (_shell == null)
                _shell = CreateShellSprite(256);
            return _shell;
        }
    }

    /// <summary>细圆环：硬球壳碎掉时那一圈往外散的线</summary>
    public static Sprite Ring
    {
        get
        {
            if (_ring == null)
                _ring = CreateRingSprite(256, 0.12f);
            return _ring;
        }
    }

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

    /// <summary>
    /// 商店货币：经典绿钞。这是本项目唯一一张外部图（其余全是代码生成），
    /// 放在 Assets/Resources/UI/banknote.png。
    ///
    /// 它和金币刻意长得不一样：金币是无描边的金属分区，钞票是深描边的像素画。
    /// 兑换关系（一枚金币 = 10 块钱）不用解释 —— 金币上本来就印着 $。
    /// </summary>
    public static Texture2D Banknote
    {
        get
        {
            if (_banknote == null)
                _banknote = Resources.Load<Texture2D>("UI/banknote");
            return _banknote;
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
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
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
        tex.filterMode = FilterMode.Trilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateDollarSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
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
        tex.filterMode = FilterMode.Trilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    /// <summary>
    /// 球。注意这里必须开 mipmap：贴图是 256px，但球在屏幕上只有 20-30px，
    /// 缩小十倍以上。没有 mipmap 时 GPU 只能在原图上点采样，边缘会闪、会有锯齿 ——
    /// 这不是"分辨率低"，是缩小采样的走样。开了 mipmap + Trilinear 之后边缘就干净了。
    /// </summary>
    /// <summary>圆环，thickness 是环宽占半径的比例。内外两边都做一像素的抗锯齿</summary>
    static Sprite CreateRingSprite(int resolution, float thickness)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        float center = resolution / 2f;
        float outer = center - 1f;
        float inner = outer * (1f - thickness);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(outer - dist) * Mathf.Clamp01(dist - inner);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Trilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateCircleSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
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
        tex.filterMode = FilterMode.Trilinear;
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
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
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
        tex.filterMode = FilterMode.Trilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateRainbowSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        float center = resolution / 2f;
        float radius = center - 1f;
        const int sectors = 6;

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
                float ang = Mathf.Atan2(dy, dx);
                if (ang < 0f) ang += Mathf.PI * 2f;
                int sector = Mathf.FloorToInt(ang / (Mathf.PI * 2f) * sectors) % sectors;
                Color c = Color.HSVToRGB(sector / (float)sectors, 0.8f, 0.92f);
                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Trilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateShellSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        float center = resolution / 2f;
        float radius = center - 1f;
        float gapOuter = radius * 0.78f;
        float gapInner = radius * 0.62f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist);
                if (dist < gapOuter && dist > gapInner) alpha *= 0.12f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Trilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    // ---- 门（白模）----
    // 门的尺寸全在 transform 的缩放里（碰撞体固定 1×1），贴图如果是正方形，花纹会被拉成扁的。
    // 所以按门的长宽比生成贴图（按比例取整后缓存），花纹和描边在任何门上粗细一致。
    // 三种：普通关着的门 = 斜条纹；联动门 = 横杠；开着的门 = 空心框（让玩家知道这里有扇门）。

    static readonly System.Collections.Generic.Dictionary<string, Sprite> doorCache =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    public static Sprite DoorClosed(float aspect) => DoorSprite("closed", aspect, 0);
    public static Sprite DoorLinked(float aspect) => DoorSprite("linked", aspect, 1);
    public static Sprite DoorOpen(float aspect) => DoorSprite("open", aspect, 2);

    static Sprite DoorSprite(string kind, float aspect, int style)
    {
        aspect = Mathf.Clamp(aspect, 0.2f, 12f);
        int h = 64;
        int w = Mathf.Clamp(Mathf.RoundToInt(h * aspect), 16, 768);
        string key = kind + ":" + w;
        if (doorCache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        const int ss = 3;                       // 每个像素 3×3 超采样，花纹边缘才干净
        float border = 3.5f;                    // 描边粗细（像素）
        float stripe = 5f;                      // 条纹粗细
        float period = 14f;                     // 条纹间距
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cov = 0f;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float px = x + (sx + 0.5f) / ss;
                        float py = y + (sy + 0.5f) / ss;
                        bool onBorder = px < border || py < border || px > w - border || py > h - border;
                        bool on;
                        if (style == 2) on = onBorder;
                        else if (onBorder) on = true;
                        else if (style == 0)
                        {
                            float d = (px + py) % period;             // 45° 斜条纹
                            on = d < stripe;
                        }
                        else
                        {
                            float d = py % period;                    // 横杠
                            on = d < stripe;
                        }
                        if (on) cov += 1f;
                    }
                cov /= ss * ss;
                // 条纹之间不是全透明，留一点底，否则门看起来像一堆散的线
                float alpha = style == 2 ? cov : Mathf.Max(cov, 0.22f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Trilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        // 贴图是 w×h 像素。门的 transform 缩放已经是 (宽, 高)，所以 sprite 本身必须是 1×1 单位：
        // Door 把 SpriteRenderer 设成 Sliced 模式、size = (1,1)，贴图整张拉进这个 1×1 的框里，
        // 而贴图的长宽比和门一致，拉完花纹不变形。FullRect 网格是 Sliced 模式的要求。
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, h, 0,
            SpriteMeshType.FullRect);
        doorCache[key] = sprite;
        return sprite;
    }

    static Sprite CreateSquareSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }

    static Sprite CreateCrosshatchSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
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
        tex.filterMode = FilterMode.Trilinear;
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, resolution);
    }
}
