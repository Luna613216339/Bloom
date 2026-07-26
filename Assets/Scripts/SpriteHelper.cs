using UnityEngine;

public static class SpriteHelper
{
    private static Sprite _circle;
    private static Sprite _square;
    private static Sprite _crosshatch;
    private static Sprite _stripedCircle;

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
